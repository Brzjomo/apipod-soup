using APIPodSoup.Core.Models;
using Microsoft.Extensions.Options;
using TOS;
using TOS.Error;
using TOS.Model;

namespace APIPodSoup.Core.Services;

public class VolcengineOssService : IOssService
{
    private readonly IOptionsMonitor<AppSettings> _settings;

    public VolcengineOssService(IOptionsMonitor<AppSettings> settings)
    {
        _settings = settings;
    }

    private OssSettings CurrentOss => _settings.CurrentValue.Oss;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(CurrentOss.AccessKey) &&
        !string.IsNullOrWhiteSpace(CurrentOss.SecretKey) &&
        !string.IsNullOrWhiteSpace(CurrentOss.Endpoint) &&
        !string.IsNullOrWhiteSpace(CurrentOss.Region) &&
        !string.IsNullOrWhiteSpace(CurrentOss.BucketName);

    public string StatusMessage
    {
        get
        {
            if (!IsConfigured)
            {
                var missing = new List<string>();
                if (string.IsNullOrWhiteSpace(CurrentOss.AccessKey)) missing.Add("AccessKey");
                if (string.IsNullOrWhiteSpace(CurrentOss.SecretKey)) missing.Add("SecretKey");
                if (string.IsNullOrWhiteSpace(CurrentOss.Endpoint)) missing.Add("Endpoint");
                if (string.IsNullOrWhiteSpace(CurrentOss.Region)) missing.Add("Region");
                if (string.IsNullOrWhiteSpace(CurrentOss.BucketName)) missing.Add("BucketName");
                return missing.Count > 0
                    ? $"OSS not configured: missing {string.Join(", ", missing)}"
                    : "OSS not configured";
            }
            return $"OSS: {CurrentOss.BucketName} @ {CurrentOss.Endpoint}";
        }
    }

    public async Task<string> UploadFileAsync(string localFilePath, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException($"OSS is not configured. Missing fields. Status: {StatusMessage}");

        var client = TosClientBuilder.Builder()
            .SetAk(CurrentOss.AccessKey)
            .SetSk(CurrentOss.SecretKey)
            .SetEndpoint(CurrentOss.Endpoint)
            .SetRegion(CurrentOss.Region)
            .Build();

        var fileName = Path.GetFileName(localFilePath);
        var objectKey = $"apipodsoup/{DateTime.UtcNow:yyyyMMdd}/{Guid.NewGuid():N}_{fileName}";

        await using var fileStream = File.OpenRead(localFilePath);
        var putObjectInput = new PutObjectInput
        {
            Bucket = CurrentOss.BucketName,
            Key = objectKey,
            Content = fileStream,
        };

        try
        {
            var result = client.PutObject(putObjectInput);

            // TOS uses virtual-hosted style by default:
            //   https://{bucket}.{endpoint_host}/{key}
            var endpointHost = CurrentOss.Endpoint
                .Replace("https://", "").Replace("http://", "").TrimEnd('/');
            var publicUrl = $"https://{CurrentOss.BucketName}.{endpointHost}/{objectKey}";
            return publicUrl;
        }
        catch (TosServerException ex)
        {
            throw new InvalidOperationException(
                $"OSS upload failed (server): {ex.Message}\nRequest ID: {ex.RequestID}\n" +
                $"Check: Endpoint={CurrentOss.Endpoint}, Region={CurrentOss.Region}, Bucket={CurrentOss.BucketName}", ex);
        }
        catch (TosClientException ex)
        {
            throw new InvalidOperationException(
                $"OSS upload failed (client): {ex.Message}\n" +
                $"Check network and credentials.", ex);
        }
    }
}

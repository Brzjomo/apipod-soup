using APIPodSoup.Core.Models;
using Microsoft.Extensions.Options;

namespace APIPodSoup.Core.Services;

public class DownloadService : IDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<AppSettings> _settings;

    public DownloadService(HttpClient httpClient, IOptionsMonitor<AppSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    public string DefaultDownloadDir =>
        string.IsNullOrWhiteSpace(_settings.CurrentValue.DownloadDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "APIPodSoup")
            : _settings.CurrentValue.DownloadDirectory;

    private const int MaxDownloadRetries = 3;

    public async Task<List<string>> DownloadFilesAsync(
        List<string> urls, string? fileNamePrefix = null, CancellationToken ct = default)
    {
        Directory.CreateDirectory(DefaultDownloadDir);

        var results = new List<string>();
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

        for (int i = 0; i < urls.Count; i++)
        {
            var url = urls[i];
            string? localPath = null;

            for (int retry = 0; retry < MaxDownloadRetries; retry++)
            {
                try
                {
                    var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                    response.EnsureSuccessStatusCode();

                    var ext = GetExtension(response);

                    string fileName;
                    if (!string.IsNullOrWhiteSpace(fileNamePrefix))
                    {
                        var suffix = urls.Count > 1 ? $"_{i + 1}" : "";
                        fileName = SanitizeFileName($"{fileNamePrefix}{suffix}_{timestamp}{ext}");
                    }
                    else
                    {
                        fileName = GetOriginalFileName(url, response);
                    }

                    localPath = Path.Combine(DefaultDownloadDir, fileName);

                    using var memoryStream = new MemoryStream();
                    await using var stream = await response.Content.ReadAsStreamAsync(ct);
                    await stream.CopyToAsync(memoryStream, ct);
                    var data = memoryStream.ToArray();

                    await File.WriteAllBytesAsync(localPath, data, ct);

                    results.Add(localPath);

                    System.Diagnostics.Debug.WriteLine(
                        retry > 0
                            ? $"[Download] Retry #{retry} succeeded: {fileName}"
                            : $"[Download] Downloaded: {fileName}");
                    break;
                }
                catch (Exception ex) when (ex is not OperationCanceledException && retry < MaxDownloadRetries - 1)
                {
                    var delay = (int)Math.Pow(2, retry + 1) * 1000;
                    System.Diagnostics.Debug.WriteLine(
                        $"[Download] Retry #{retry + 1} for {url} in {delay}ms: {ex.Message}");
                    await Task.Delay(delay, ct);
                }
            }

            if (localPath == null)
                throw new IOException($"Failed to download result after {MaxDownloadRetries} attempts: {url}");
        }

        return results;
    }

    private static string GetExtension(HttpResponseMessage response)
    {
        return response.Content.Headers.ContentType?.MediaType switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "video/mp4" => ".mp4",
            "video/quicktime" => ".mov",
            "video/webm" => ".webm",
            _ => ".bin"
        };
    }

    private static string GetOriginalFileName(string url, HttpResponseMessage response)
    {
        var disposition = response.Content.Headers.ContentDisposition;
        if (disposition?.FileName != null)
            return SanitizeFileName(disposition.FileName);

        var uri = new Uri(url);
        var name = Path.GetFileName(uri.AbsolutePath);
        if (!string.IsNullOrWhiteSpace(name) && name.Contains('.'))
            return SanitizeFileName(name);

        return $"{DateTime.UtcNow:yyyyMMddHHmmssfff}{GetExtension(response)}";
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}

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

    public async Task<List<string>> DownloadFilesAsync(
        List<string> urls, string? fileNamePrefix = null, CancellationToken ct = default)
    {
        Directory.CreateDirectory(DefaultDownloadDir);

        var results = new List<string>();

        for (int i = 0; i < urls.Count; i++)
        {
            var url = urls[i];
            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var ext = GetExtension(response);
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

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

            var localPath = Path.Combine(DefaultDownloadDir, fileName);

            using var memoryStream = new MemoryStream();
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            await stream.CopyToAsync(memoryStream, ct);
            var data = memoryStream.ToArray();

            await File.WriteAllBytesAsync(localPath, data, ct);

            results.Add(localPath);
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

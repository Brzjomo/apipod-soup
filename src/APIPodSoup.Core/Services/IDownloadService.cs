namespace APIPodSoup.Core.Services;

public interface IDownloadService
{
    Task<List<string>> DownloadFilesAsync(List<string> urls, string? fileNamePrefix = null, CancellationToken ct = default);
    string DefaultDownloadDir { get; }
}

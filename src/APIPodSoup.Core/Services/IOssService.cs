namespace APIPodSoup.Core.Services;

public interface IOssService
{
    Task<string> UploadFileAsync(string localFilePath, CancellationToken ct = default);
    bool IsConfigured { get; }
    string StatusMessage { get; }
}

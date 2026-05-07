namespace APIPodSoup.Core.Models;

public class AppSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://api.apipod.ai";
    public OssSettings Oss { get; set; } = new();
    public string DownloadDirectory { get; set; } = string.Empty;
    public string Theme { get; set; } = "Dark";
    public string? ProxyUrl { get; set; }
}

public class OssSettings
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
}

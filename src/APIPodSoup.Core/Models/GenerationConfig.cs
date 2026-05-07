namespace APIPodSoup.Core.Models;

public class GenerationConfig
{
    public string ModelId { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string AspectRatio { get; set; } = "1:1";
    public string Quality { get; set; } = "1K";
    public List<string> ReferenceImagePaths { get; set; } = [];
    public string? CallbackUrl { get; set; }
}

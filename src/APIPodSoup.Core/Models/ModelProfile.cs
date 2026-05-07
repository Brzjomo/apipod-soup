using APIPodSoup.Core.Enums;

namespace APIPodSoup.Core.Models;

public class ModelProfile
{
    public string ModelId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public ModelCategory Category { get; set; }
    public int MaxReferenceImages { get; set; }
    public bool RequireReferenceImage { get; set; }
    public string[] SupportedAspectRatios { get; set; } = [];
    public string[] SupportedQualities { get; set; } = [];
    public string PromptLabel { get; set; } = "Prompt";
    public string ReferenceLabel { get; set; } = "Reference Images (optional)";
    public int MaxPromptLength { get; set; } = 4000;
    public string ApiEndpoint { get; set; } = "/v1/images/generations";
    public string[] SupportedImageFormats { get; set; } = [".png", ".jpg", ".jpeg", ".webp"];
}

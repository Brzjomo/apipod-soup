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

    /// <summary>Minimum video duration in seconds. 0 means duration is not configurable.</summary>
    public int MinDuration { get; set; }

    /// <summary>Maximum video duration in seconds. 0 means duration is not configurable.</summary>
    public int MaxDuration { get; set; }

    /// <summary>Whether the duration control should be visible (true when MinDuration > 0 and MaxDuration > 0).</summary>
    public bool ShowDuration => MinDuration > 0 && MaxDuration > MinDuration;
}

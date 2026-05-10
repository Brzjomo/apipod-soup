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

    /// <summary>Maximum number of output images. 0 means not configurable.</summary>
    public int MaxOutputCount { get; set; }

    /// <summary>Whether the output count control should be visible.</summary>
    public bool ShowOutputCount => MaxOutputCount > 1;

    /// <summary>Use "size" as the resolution param key instead of "quality".</summary>
    public bool UsesSizeParam { get; set; }

    /// <summary>Whether the model supports a thinking/tool-use mode toggle.</summary>
    public bool SupportsThinkingMode { get; set; }

    /// <summary>Whether the thinking mode toggle should be visible.</summary>
    public bool ShowThinkingMode => SupportsThinkingMode;
}

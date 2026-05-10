using APIPodSoup.Core.Enums;
using APIPodSoup.Core.Models;

namespace APIPodSoup.Core.Services;

public interface IModelProfileProvider
{
    List<ModelProfile> GetProfiles();
    List<ModelProfile> GetProfilesByCategory(ModelCategory category);
    ModelProfile? GetProfile(string modelId);
}

public class ModelProfileProvider : IModelProfileProvider
{
    private readonly List<ModelProfile> _profiles;

    public ModelProfileProvider()
    {
        _profiles =
        [
            // ---- Image Generation ----
            new ModelProfile
            {
                ModelId = "nano-banana-2",
                DisplayName = "Nano Banana 2",
                Category = ModelCategory.ImageGeneration,
                MaxReferenceImages = 14,
                RequireReferenceImage = false,
                SupportedAspectRatios = ["1:1", "1:4", "1:8", "2:3", "3:2", "3:4", "4:1", "4:3", "4:5", "5:4", "8:1", "9:16", "16:9", "21:9"],
                SupportedQualities = ["512px", "1K", "2K", "4K"],
                PromptLabel = "Image Description",
                ReferenceLabel = "Reference Images (optional, max 14)",
                MaxPromptLength = 4000,
                ApiEndpoint = "/v1/images/generations",
            },
            new ModelProfile
            {
                ModelId = "nano-banana-pro",
                DisplayName = "Nano Banana Pro",
                Category = ModelCategory.ImageGeneration,
                MaxReferenceImages = 8,
                RequireReferenceImage = false,
                SupportedAspectRatios = ["1:1", "2:3", "3:2", "3:4", "4:3", "4:5", "5:4", "9:16", "16:9", "21:9"],
                SupportedQualities = ["1K", "2K", "4K"],
                PromptLabel = "Image Description",
                ReferenceLabel = "Reference Images (optional, max 8)",
                MaxPromptLength = 4000,
                ApiEndpoint = "/v1/images/generations",
            },
            new ModelProfile
            {
                ModelId = "gpt-image-2",
                DisplayName = "GPT Image 2",
                Category = ModelCategory.ImageGeneration,
                MaxReferenceImages = 0,
                RequireReferenceImage = false,
                SupportedAspectRatios = ["1:1", "2:3", "3:2", "3:4", "4:3", "4:5", "5:4", "9:16", "16:9", "21:9"],
                SupportedQualities = ["1K", "2K", "4K"],
                PromptLabel = "Image Description",
                ReferenceLabel = "Reference Images (not supported)",
                MaxPromptLength = 4000,
                ApiEndpoint = "/v1/images/generations",
            },
            new ModelProfile
            {
                ModelId = "gpt-image-2-edit",
                DisplayName = "GPT Image 2 Edit",
                Category = ModelCategory.ImageGeneration,
                MaxReferenceImages = 6,
                RequireReferenceImage = true,
                SupportedAspectRatios = ["1:1", "2:3", "3:2", "3:4", "4:3", "4:5", "5:4", "9:16", "16:9", "21:9"],
                SupportedQualities = [],
                PromptLabel = "Edit Description",
                ReferenceLabel = "Reference Images (required, max 6)",
                MaxPromptLength = 4000,
                ApiEndpoint = "/v1/images/generations",
            },

            // ---- Video Generation ----
            new ModelProfile
            {
                ModelId = "grok-imagine-i2v",
                DisplayName = "Grok Imagine I2V",
                Category = ModelCategory.VideoGeneration,
                MaxReferenceImages = 7,
                RequireReferenceImage = true,
                SupportedAspectRatios = ["2:3", "3:2", "1:1", "16:9", "9:16"],
                SupportedQualities = ["480p", "720p"],
                PromptLabel = "Video Description",
                ReferenceLabel = "Reference Images (required, max 7)",
                MaxPromptLength = 4000,
                ApiEndpoint = "/v1/videos/generations",
                MinDuration = 6,
                MaxDuration = 30,
            },
            new ModelProfile
            {
                ModelId = "veo3-1-fast",
                DisplayName = "Veo 3.1 Fast",
                Category = ModelCategory.VideoGeneration,
                MaxReferenceImages = 2,
                RequireReferenceImage = false,
                SupportedAspectRatios = ["16:9", "9:16"],
                SupportedQualities = [],   // Veo has no quality/resolution options
                PromptLabel = "Video Description",
                ReferenceLabel = "Reference Images (optional, max 2 — start/end frame)",
                MaxPromptLength = 4000,
                ApiEndpoint = "/v1/videos/generations",
            },
            new ModelProfile
            {
                ModelId = "veo3-1-fast-ref",
                DisplayName = "Veo 3.1 Fast Ref",
                Category = ModelCategory.VideoGeneration,
                MaxReferenceImages = 3,
                RequireReferenceImage = false,
                SupportedAspectRatios = ["16:9", "9:16"],
                SupportedQualities = [],
                PromptLabel = "Video Description",
                ReferenceLabel = "Reference Images (optional, max 3)",
                MaxPromptLength = 4000,
                ApiEndpoint = "/v1/videos/generations",
            },
        ];
    }

    public List<ModelProfile> GetProfiles() => _profiles;

    public List<ModelProfile> GetProfilesByCategory(ModelCategory category) =>
        _profiles.Where(p => p.Category == category).ToList();

    public ModelProfile? GetProfile(string modelId) =>
        _profiles.FirstOrDefault(p => p.ModelId == modelId);
}

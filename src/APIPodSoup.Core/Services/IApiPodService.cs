using APIPodSoup.Core.Models;

namespace APIPodSoup.Core.Services;

public interface IApiPodService
{
    // Image generation
    Task<SubmitTaskResponse> SubmitImageGenerationAsync(ImageGenerationRequest request, CancellationToken ct = default);
    Task<TaskDetailResponse> GetTaskStatusAsync(string taskId, CancellationToken ct = default);

    // Video generation (paths auto-constructed from base URL in settings)
    Task<SubmitTaskResponse> SubmitVideoGenerationAsync(ImageGenerationRequest request, CancellationToken ct = default);
    Task<TaskDetailResponse> GetVideoTaskStatusAsync(string taskId, CancellationToken ct = default);

    // Polling
    Task<TaskDetailResponse> WaitForCompletionAsync(string taskId, int pollIntervalMs = 2000, CancellationToken ct = default);
    Task<TaskDetailResponse> WaitForVideoCompletionAsync(string taskId, int pollIntervalMs = 2000, CancellationToken ct = default);
}

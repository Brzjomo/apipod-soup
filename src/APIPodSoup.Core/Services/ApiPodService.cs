using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using APIPodSoup.Core.Models;
using Microsoft.Extensions.Options;

namespace APIPodSoup.Core.Services;

public class ApiPodService : IApiPodService
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<AppSettings> _settings;
    private string _lastBaseUrl = string.Empty;

    public ApiPodService(HttpClient httpClient, IOptionsMonitor<AppSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    /// <summary>
    /// Reads the user-configured base URL, normalizes it (strips trailing slash),
    /// and updates HttpClient.BaseAddress. Re-applies when the URL changes.
    /// </summary>
    private void EnsureBaseAddress()
    {
        var raw = _settings.CurrentValue.ApiBaseUrl;
        if (string.IsNullOrWhiteSpace(raw))
            raw = "https://api.apipod.ai";

        // Normalize: strip trailing slash so HttpClient combines correctly
        // e.g. "https://api.apipod.ai/" -> "https://api.apipod.ai"
        // Then "/v1/images/generations" -> "https://api.apipod.ai/v1/images/generations"
        var normalized = raw.TrimEnd('/');

        if (normalized != _lastBaseUrl || _httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(normalized + "/");
            _lastBaseUrl = normalized;
        }
    }

    // ---- Image generation (text-to-image, image-to-image) ----

    public async Task<SubmitTaskResponse> SubmitImageGenerationAsync(
        ImageGenerationRequest request, CancellationToken ct = default)
    {
        EnsureBaseAddress();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("v1/images/generations", content, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SubmitTaskResponse>(cancellationToken: ct))!;
    }

    public async Task<TaskDetailResponse> GetTaskStatusAsync(string taskId, CancellationToken ct = default)
    {
        EnsureBaseAddress();
        var response = await _httpClient.GetAsync($"v1/images/status/{taskId}", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskDetailResponse>(cancellationToken: ct))!;
    }

    // ---- Video generation (placeholder — API docs pending) ----

    public async Task<SubmitTaskResponse> SubmitVideoGenerationAsync(
        ImageGenerationRequest request, CancellationToken ct = default)
    {
        EnsureBaseAddress();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("v1/videos/generations", content, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SubmitTaskResponse>(cancellationToken: ct))!;
    }

    public async Task<TaskDetailResponse> GetVideoTaskStatusAsync(string taskId, CancellationToken ct = default)
    {
        EnsureBaseAddress();
        var response = await _httpClient.GetAsync($"v1/videos/status/{taskId}", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskDetailResponse>(cancellationToken: ct))!;
    }

    // ---- Polling ----

    public async Task<TaskDetailResponse> WaitForCompletionAsync(
        string taskId, int pollIntervalMs = 2000, CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var result = await GetTaskStatusAsync(taskId, ct);

            if (result.Code != 200)
                throw new InvalidOperationException($"Task query failed: {result.Message}");

            var status = result.Data?.Status?.ToLowerInvariant();
            if (status == "completed")
                return result;
            if (status == "failed")
                throw new InvalidOperationException($"Task failed: {result.Data?.Error ?? "Unknown error"}");
            if (status == "cancelled")
                throw new OperationCanceledException("Task was cancelled by the server.");

            await Task.Delay(pollIntervalMs, ct);
        }

        throw new OperationCanceledException(ct);
    }

    public async Task<TaskDetailResponse> WaitForVideoCompletionAsync(
        string taskId, int pollIntervalMs = 2000, CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var result = await GetVideoTaskStatusAsync(taskId, ct);

            if (result.Code != 200)
                throw new InvalidOperationException($"Video task query failed: {result.Message}");

            var status = result.Data?.Status?.ToLowerInvariant();
            if (status == "completed")
                return result;
            if (status == "failed")
                throw new InvalidOperationException($"Video task failed: {result.Data?.Error ?? "Unknown error"}");
            if (status == "cancelled")
                throw new OperationCanceledException("Video task was cancelled by the server.");

            await Task.Delay(pollIntervalMs, ct);
        }

        throw new OperationCanceledException(ct);
    }
}

using System.Text.Json.Serialization;

namespace APIPodSoup.Core.Models;

public class ImageGenerationRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("aspect_ratio")]
    public string AspectRatio { get; set; } = "1:1";

    [JsonPropertyName("quality")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Quality { get; set; }

    [JsonPropertyName("resolution")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Resolution { get; set; }

    [JsonPropertyName("image_urls")]
    public List<string> ImageUrls { get; set; } = [];

    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }

    [JsonPropertyName("duration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Duration { get; set; }
}

public class SubmitTaskResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public SubmitTaskData? Data { get; set; }
}

public class SubmitTaskData
{
    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;
}

public class TaskDetailResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public TaskDetailData? Data { get; set; }
}

public class TaskDetailData
{
    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Array of generated result URLs (the actual API field name is "result").</summary>
    [JsonPropertyName("result")]
    public List<string>? ResultUrls { get; set; }

    [JsonPropertyName("error_message")]
    public string? Error { get; set; }

    [JsonPropertyName("completed_at")]
    public long? CompletedAt { get; set; }
}

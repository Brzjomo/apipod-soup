using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIPodSoup.Core.Models;

public class HistoryRecord
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string ModelType { get; set; } = string.Empty;

    public string ModelDisplayName { get; set; } = string.Empty;

    public string Prompt { get; set; } = string.Empty;

    public string AspectRatio { get; set; } = string.Empty;

    public string Quality { get; set; } = string.Empty;

    /// <summary>Local paths of uploaded reference images (JSON array).</summary>
    public string? ReferenceImagePaths { get; set; }

    /// <summary>OSS URLs after upload (JSON array).</summary>
    public string? OssUrls { get; set; }

    /// <summary>Full API request body JSON for audit/replay.</summary>
    public string? ApiRequestJson { get; set; }

    /// <summary>Full API response JSON.</summary>
    public string? ApiResponseJson { get; set; }

    public string? TaskId { get; set; }

    /// <summary>Result URLs returned by API (JSON array).</summary>
    public string? ResultUrls { get; set; }

    /// <summary>Local file paths after download (JSON array).</summary>
    public string? LocalResultPaths { get; set; }

    public string Status { get; set; } = "pending";

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}

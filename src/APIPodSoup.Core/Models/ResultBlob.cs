using System.ComponentModel.DataAnnotations;

namespace APIPodSoup.Core.Models;

public class ResultBlob
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string HistoryRecordId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public byte[] Data { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

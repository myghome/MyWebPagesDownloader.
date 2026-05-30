using MyWebPagesDownloader.Core.Enums;

namespace MyWebPagesDownloader.Core.Entities;

public sealed class DownloadRecord
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Url { get; init; } = string.Empty;
    public DownloadStatus Status { get; set; } = DownloadStatus.Pending;
    public DownloadPriority Priority { get; set; } = DownloadPriority.Normal;
    public int AttemptCount { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? HttpStatusCode { get; set; }
    public long? ContentLength { get; set; }
    public long? ElapsedMilliseconds { get; set; }
    public string? FilePath { get; set; }
    public string? ErrorMessage { get; set; }
}

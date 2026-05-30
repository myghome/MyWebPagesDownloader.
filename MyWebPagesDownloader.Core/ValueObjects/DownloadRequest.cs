using MyWebPagesDownloader.Core.Enums;

namespace MyWebPagesDownloader.Core.ValueObjects;

public sealed record DownloadRequest(
    Guid Id,
    Uri Url,
    DownloadPriority Priority = DownloadPriority.Normal,
    string? CorrelationId = null,
    int Attempt = 0);

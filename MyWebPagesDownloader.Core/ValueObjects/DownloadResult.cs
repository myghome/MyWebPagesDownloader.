using System.Net;

namespace MyWebPagesDownloader.Core.ValueObjects;

public sealed record DownloadResult(
    bool Success,
    HttpStatusCode? StatusCode = null,
    long? ContentLength = null,
    TimeSpan? Duration = null,
    string? ErrorMessage = null);

namespace MyWebPagesDownloader.Core.Enums;

public enum DownloadStatus
{
    Pending = 0,
    Queued = 1,
    Running = 2,
    Retrying = 3,
    Succeeded = 4,
    Failed = 5,
    Cancelled = 6,
    TimedOut = 7
}

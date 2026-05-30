namespace MyWebPagesDownloader.Core.Contracts;

public interface IDownloadMetrics
{
    void IncrementSuccess();
    void IncrementFailure();
    void RecordDuration(long milliseconds);
    int SuccessCount { get; }
    int FailureCount { get; }
    long TotalDurationMilliseconds { get; }
}

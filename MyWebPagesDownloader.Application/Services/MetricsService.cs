using MyWebPagesDownloader.Core.Contracts;

namespace MyWebPagesDownloader.Application.Services;

public sealed class MetricsService : IDownloadMetrics
{
    private int _successCount;
    private int _failureCount;
    private long _totalDurationMilliseconds;
    private readonly object _lockObject = new();

    public int SuccessCount
    {
        get
        {
            lock (_lockObject) return _successCount;
        }
    }

    public int FailureCount
    {
        get
        {
            lock (_lockObject) return _failureCount;
        }
    }

    public long TotalDurationMilliseconds
    {
        get
        {
            lock (_lockObject) return _totalDurationMilliseconds;
        }
    }

    public void IncrementSuccess()
    {
        lock (_lockObject) _successCount++;
    }

    public void IncrementFailure()
    {
        lock (_lockObject) _failureCount++;
    }

    public void RecordDuration(long milliseconds)
    {
        lock (_lockObject) _totalDurationMilliseconds += milliseconds;
    }

    public void Reset()
    {
        lock (_lockObject)
        {
            _successCount = 0;
            _failureCount = 0;
            _totalDurationMilliseconds = 0;
        }
    }
}

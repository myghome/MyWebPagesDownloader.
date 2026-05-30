namespace MyWebPagesDownloader.Application.Services;

public sealed class HealthService
{
    private int _queueDepth;
    private int _activeWorkers;
    private long _memoryUsageMb;
    private readonly object _lockObject = new();

    public int QueueDepth
    {
        get
        {
            lock (_lockObject) return _queueDepth;
        }
        set
        {
            lock (_lockObject) _queueDepth = value;
        }
    }

    public int ActiveWorkers
    {
        get
        {
            lock (_lockObject) return _activeWorkers;
        }
        set
        {
            lock (_lockObject) _activeWorkers = value;
        }
    }

    public long MemoryUsageMb
    {
        get
        {
            lock (_lockObject) return _memoryUsageMb;
        }
        set
        {
            lock (_lockObject) _memoryUsageMb = value;
        }
    }

    public void UpdateMemory()
    {
        var workingSet = GC.GetTotalMemory(false) / (1024 * 1024);
        MemoryUsageMb = workingSet;
    }

    public string GetStatus()
    {
        UpdateMemory();
        return $"[Health] Queue: {QueueDepth}, Workers: {ActiveWorkers}, Memory: {MemoryUsageMb}MB";
    }
}

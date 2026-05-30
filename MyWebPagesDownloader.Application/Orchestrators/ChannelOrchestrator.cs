using MyWebPagesDownloader.Core.Contracts;
using MyWebPagesDownloader.Core.ValueObjects;
using MyWebPagesDownloader.Application.Services;
using MyWebPagesDownloader.Application.Policies;
using MyWebPagesDownloader.Application.Workers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyWebPagesDownloader.Application.Options;

namespace MyWebPagesDownloader.Application.Orchestrators;

public sealed class ChannelOrchestrator
{
    private readonly IDownloadQueue _queue;
    private readonly IDownloadProvider _provider;
    private readonly IDownloadRepository _repository;
    private readonly IContentStore _contentStore;
    private readonly IDownloadMetrics _metrics;
    private readonly HealthService _health;
    private readonly RateLimiterService _rateLimiter;
    private readonly RetryPolicyFactory _policyFactory;
    private readonly DownloaderOptions _options;
    private readonly ILogger<ChannelOrchestrator> _logger;

    public ChannelOrchestrator(
        IDownloadQueue queue,
        IDownloadProvider provider,
        IDownloadRepository repository,
        IContentStore contentStore,
        IDownloadMetrics metrics,
        HealthService health,
        RateLimiterService rateLimiter,
        RetryPolicyFactory policyFactory,
        IOptions<DownloaderOptions> options,
        ILogger<ChannelOrchestrator> logger)
    {
        _queue = queue;
        _provider = provider;
        _repository = repository;
        _contentStore = contentStore;
        _metrics = metrics;
        _health = health;
        _rateLimiter = rateLimiter;
        _policyFactory = policyFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnqueueAsync(IEnumerable<Uri> urls, CancellationToken cancellationToken)
    {
        foreach (var url in urls)
        {
            var request = new DownloadRequest(
                Id: Guid.NewGuid(),
                Url: url,
                CorrelationId: Guid.NewGuid().ToString("N")[..8]);

            await _queue.EnqueueAsync(request, cancellationToken);
            _health.QueueDepth++;
            _logger.LogInformation("[{CorrelationId}] Enqueued: {Url}", request.CorrelationId, url);
        }
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var workers = Enumerable.Range(0, _options.MaxConcurrentWorkers)
            .Select(id => ProcessWorkerAsync(id, cancellationToken))
            .ToList();

        await Task.WhenAll(workers);
        _logger.LogInformation("All workers completed.");
    }

    private async Task ProcessWorkerAsync(int workerId, CancellationToken cancellationToken)
    {
        var workerLogger = new SimpleLogger($"Worker-{workerId}", _logger);
        var worker = new DownloadWorker(
            _provider, _repository, _contentStore, _metrics, _policyFactory, workerLogger);

        try
        {
            _health.ActiveWorkers++;
            _logger.LogInformation("Worker {WorkerId} started.", workerId);

            await foreach (var request in _queue.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await _rateLimiter.AcquireAsync(cancellationToken);
                    _health.QueueDepth--;
                    await worker.ProcessAsync(request, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Worker {WorkerId} cancelled.", workerId);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker {WorkerId} error processing {Url}", workerId, request.Url);
                }
            }
        }
        finally
        {
            _health.ActiveWorkers--;
            _logger.LogInformation("Worker {WorkerId} stopped.", workerId);
        }
    }

    private sealed class SimpleLogger : ILogger<DownloadWorker>
    {
        private readonly string _prefix;
        private readonly ILogger _baseLogger;

        public SimpleLogger(string prefix, ILogger baseLogger)
        {
            _prefix = prefix;
            _baseLogger = baseLogger;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _baseLogger.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => _baseLogger.IsEnabled(logLevel);
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => _baseLogger.Log(logLevel, eventId, state, exception, (s, e) => $"[{_prefix}] {formatter(s, e)}");
    }
}

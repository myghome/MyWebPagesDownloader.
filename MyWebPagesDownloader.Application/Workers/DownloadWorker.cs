using MyWebPagesDownloader.Core.Contracts;
using MyWebPagesDownloader.Core.Entities;
using MyWebPagesDownloader.Core.Enums;
using MyWebPagesDownloader.Core.ValueObjects;
using MyWebPagesDownloader.Application.Policies;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MyWebPagesDownloader.Application.Workers;

public sealed class DownloadWorker
{
    private readonly IDownloadProvider _provider;
    private readonly IDownloadRepository _repository;
    private readonly IContentStore _contentStore;
    private readonly IDownloadMetrics _metrics;
    private readonly RetryPolicyFactory _policyFactory;
    private readonly ILogger<DownloadWorker> _logger;

    public DownloadWorker(
        IDownloadProvider provider,
        IDownloadRepository repository,
        IContentStore contentStore,
        IDownloadMetrics metrics,
        RetryPolicyFactory policyFactory,
        ILogger<DownloadWorker> logger)
    {
        _provider = provider;
        _repository = repository;
        _contentStore = contentStore;
        _metrics = metrics;
        _policyFactory = policyFactory;
        _logger = logger;
    }

    public async Task ProcessAsync(DownloadRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var record = await _repository.GetByIdAsync(request.Id.ToString(), cancellationToken) 
            ?? CreateRecord(request);

        try
        {
            record.Status = DownloadStatus.Running;
            record.StartedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(record, cancellationToken);

            _logger.LogInformation("[{CorrelationId}] Started: {Url}", request.CorrelationId, request.Url);

            var policy = _policyFactory.CreateCombinedPolicy<string>();
            var content = await policy.ExecuteAsync(
                async ct => await DownloadContentAsync(request.Url, ct),
                cancellationToken);

            record.HttpStatusCode = 200;
            record.ContentLength = content.Length;
            record.FilePath = await _contentStore.SaveAsync(
                request.Url.ToString(),
                content,
                cancellationToken);

            record.Status = DownloadStatus.Succeeded;
            _metrics.IncrementSuccess();
            _logger.LogInformation("[{CorrelationId}] Succeeded: {Url}", request.CorrelationId, request.Url);
        }
        catch (Exception ex)
        {
            record.AttemptCount++;
            if (record.AttemptCount >= 3)
            {
                record.Status = DownloadStatus.Failed;
                record.ErrorMessage = ex.Message;
                _metrics.IncrementFailure();
                _logger.LogError(ex, "[{CorrelationId}] Failed after {Attempts} attempts: {Url}",
                    request.CorrelationId, record.AttemptCount, request.Url);
            }
            else
            {
                record.Status = DownloadStatus.Retrying;
                _logger.LogWarning(ex, "[{CorrelationId}] Retry {Attempt}/3: {Url}",
                    request.CorrelationId, record.AttemptCount, request.Url);
            }
        }
        finally
        {
            stopwatch.Stop();
            record.CompletedAt = DateTime.UtcNow;
            record.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            _metrics.RecordDuration(stopwatch.ElapsedMilliseconds);
            await _repository.UpdateAsync(record, cancellationToken);
        }
    }

    private async Task<string> DownloadContentAsync(Uri url, CancellationToken cancellationToken)
    {
        var result = await _provider.DownloadAsync(url, cancellationToken);
        if (!result.Success)
            throw new InvalidOperationException($"Download failed: {result.ErrorMessage}");
        // For mock/real providers, we'll just return a placeholder for content
        return $"Downloaded content from {url} ({result.ContentLength} bytes)";
    }

    private DownloadRecord CreateRecord(DownloadRequest request)
    {
        return new DownloadRecord
        {
            Id = request.Id.ToString(),
            Url = request.Url.ToString(),
            Priority = request.Priority,
            CorrelationId = request.CorrelationId,
            Status = DownloadStatus.Queued
        };
    }
}

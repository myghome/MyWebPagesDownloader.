using MyWebPagesDownloader.Core.Contracts;
using MyWebPagesDownloader.Core.Enums;
using Microsoft.Extensions.Logging;

namespace MyWebPagesDownloader.Application.Services;

public sealed class RecoveryService
{
    private readonly IDownloadRepository _repository;
    private readonly ILogger<RecoveryService> _logger;

    public RecoveryService(IDownloadRepository repository, ILogger<RecoveryService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<int> RecoverIncompleteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var incomplete = await _repository.GetIncompleteAsync(cancellationToken);
            var count = incomplete.Count;

            if (count == 0)
            {
                _logger.LogInformation("No incomplete downloads to recover.");
                return 0;
            }

            _logger.LogInformation("Recovering {Count} incomplete downloads.", count);

            foreach (var record in incomplete)
            {
                record.Status = DownloadStatus.Pending;
                record.AttemptCount = 0;
                await _repository.UpdateAsync(record, cancellationToken);
                _logger.LogInformation("[{CorrelationId}] Requeued: {Url}", record.CorrelationId, record.Url);
            }

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during recovery.");
            throw;
        }
    }
}

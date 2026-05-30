using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyWebPagesDownloader.Application.Orchestrators;
using MyWebPagesDownloader.Application.Services;

namespace MyWebPagesDownloader.Application.HostedServices;

public sealed class DownloaderHostedService : BackgroundService
{
    private readonly ChannelOrchestrator _orchestrator;
    private readonly HealthService _health;
    private readonly ILogger<DownloaderHostedService> _logger;

    public DownloaderHostedService(
        ChannelOrchestrator orchestrator,
        HealthService health,
        ILogger<DownloaderHostedService> logger)
    {
        _orchestrator = orchestrator;
        _health = health;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DownloaderHostedService starting...");

        try
        {
            await _orchestrator.ProcessAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("DownloaderHostedService gracefully stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DownloaderHostedService error.");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DownloaderHostedService stopping... {Status}", _health.GetStatus());
        await base.StopAsync(cancellationToken);
    }
}

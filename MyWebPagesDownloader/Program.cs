using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using MyWebPagesDownloader.Core.Contracts;
using MyWebPagesDownloader.Application.Options;
using MyWebPagesDownloader.Application.Services;
using MyWebPagesDownloader.Application.Policies;
using MyWebPagesDownloader.Application.Orchestrators;
using MyWebPagesDownloader.Application.Queues;
using MyWebPagesDownloader.Application.HostedServices;
using MyWebPagesDownloader.Infrastructure.Configuration;
using MyWebPagesDownloader.Infrastructure.Http;
using MyWebPagesDownloader.Infrastructure.Storage;
using MyWebPagesDownloader.Infrastructure.Persistence;
using MyWebPagesDownloader.Infrastructure.Observability;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("🚀 MyWebPagesDownloader starting...");

    var builder = Host.CreateDefaultBuilder(args);

    builder.UseSerilog();

    builder.ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    });

    builder.ConfigureServices((context, services) =>
    {
        // Configuration
        services.Configure<DownloaderOptions>(context.Configuration.GetSection("Downloader"));
        services.Configure<RetryOptions>(context.Configuration.GetSection("Retry"));
        services.Configure<StorageOptions>(context.Configuration.GetSection("Storage"));

        // Core Services
        services.AddScoped<IDownloadMetrics, MetricsService>();
        services.AddScoped<HealthService>();
        services.AddScoped<RecoveryService>();
        services.AddScoped<RateLimiterService>(sp => new RateLimiterService(10));
        services.AddScoped<RetryPolicyFactory>();

        // Application Services
        services.AddScoped<IDownloadQueue, ChannelDownloadQueue>();
        services.AddScoped<ChannelOrchestrator>();

        // Infrastructure
        services.AddInfrastructureServices("downloads.db");

        // Observability
        services.AddOpenTelemetryTracing();

        // Hosted Services
        services.AddHostedService<DownloaderHostedService>();
    });

    var host = builder.Build();

    // Initialize database
    using (var scope = host.Services.CreateScope())
    {
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync();
    }

    // Graceful shutdown handling
    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) =>
    {
        e.Cancel = true;
        Log.Information("⏹️  Graceful shutdown initiated...");
        cts.Cancel();
    };

    // Run host
    await host.RunAsync(cts.Token);

    Log.Information("✅ MyWebPagesDownloader stopped gracefully.");
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Application terminated unexpectedly");
    Environment.Exit(1);
}
finally
{
    await Log.CloseAndFlushAsync();
}

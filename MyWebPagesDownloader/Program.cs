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
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
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

    // Demo: Enqueue some test URLs
    using (var scope = host.Services.CreateScope())
    {
        var orchestrator = scope.ServiceProvider.GetRequiredService<ChannelOrchestrator>();
        var testUrls = new[]
        {
            new Uri("https://httpbin.org/html"),
            new Uri("https://httpbin.org/delay/1"),
            new Uri("https://httpbin.org/status/200"),
            new Uri("https://httpbin.org/get"),
            new Uri("https://httpbin.org/user-agent"),
        };

        Log.Information("📝 Enqueueing {Count} test URLs...", testUrls.Length);
        await orchestrator.EnqueueAsync(testUrls, CancellationToken.None);
        Log.Information("✅ All URLs enqueued. Starting workers...");
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

    // Show final metrics
    using (var scope = host.Services.CreateScope())
    {
        var metrics = scope.ServiceProvider.GetRequiredService<IDownloadMetrics>();
        var health = scope.ServiceProvider.GetRequiredService<HealthService>();
        Log.Information("📊 Final Metrics:");
        Log.Information("   ✅ Success: {Success}", metrics.SuccessCount);
        Log.Information("   ❌ Failure: {Failure}", metrics.FailureCount);
        Log.Information("   ⏱️  Total Time: {Duration}ms", metrics.TotalDurationMilliseconds);
        Log.Information("   {Status}", health.GetStatus());
    }

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


using Microsoft.Extensions.DependencyInjection;
using MyWebPagesDownloader.Core.Contracts;
using MyWebPagesDownloader.Infrastructure.Http;
using MyWebPagesDownloader.Infrastructure.Storage;
using MyWebPagesDownloader.Infrastructure.Persistence;

namespace MyWebPagesDownloader.Infrastructure.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string databasePath = "downloads.db")
    {
        // Http
        services.AddHttpClient<HttpClientProvider>();
        services.AddScoped<IDownloadProvider>(sp => sp.GetRequiredService<HttpClientProvider>());

        // Storage
        services.AddScoped<IContentStore>(sp => new FileSystemContentStore("./downloads"));

        // Persistence
        services.AddScoped(sp => new DatabaseInitializer(databasePath, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DatabaseInitializer>>()));
        services.AddScoped<IDownloadRepository>(sp => new SqliteDownloadRepository(databasePath, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SqliteDownloadRepository>>()));

        return services;
    }
}

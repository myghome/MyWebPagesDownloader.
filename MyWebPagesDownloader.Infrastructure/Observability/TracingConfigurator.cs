using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.Extensions.DependencyInjection;

namespace MyWebPagesDownloader.Infrastructure.Observability;

public static class TracingConfigurator
{
    public static IServiceCollection AddOpenTelemetryTracing(this IServiceCollection services)
    {
        var resource = ResourceBuilder
            .CreateDefault()
            .AddService("MyWebPagesDownloader");

        services.AddOpenTelemetry()
            .WithTracing(tracingBuilder =>
            {
                tracingBuilder
                    .SetResourceBuilder(resource)
                    .AddHttpClientInstrumentation()
                    .AddConsoleExporter();
            });

        return services;
    }
}

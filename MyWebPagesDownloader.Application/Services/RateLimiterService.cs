using System.Threading.RateLimiting;

namespace MyWebPagesDownloader.Application.Services;

public sealed class RateLimiterService : IAsyncDisposable
{
    private readonly RateLimiter _rateLimiter;

    public RateLimiterService(int requestsPerSecond = 10)
    {
        _rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = requestsPerSecond,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 100,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            TokensPerPeriod = requestsPerSecond,
            AutoReplenishment = true
        });
    }

    public async ValueTask AcquireAsync(CancellationToken cancellationToken = default)
    {
        using var lease = await _rateLimiter.AcquireAsync(1, cancellationToken);
        if (!lease.IsAcquired)
            throw new InvalidOperationException("Failed to acquire rate limit lease.");
    }

    public async ValueTask DisposeAsync()
    {
        _rateLimiter.Dispose();
        await ValueTask.CompletedTask;
    }
}

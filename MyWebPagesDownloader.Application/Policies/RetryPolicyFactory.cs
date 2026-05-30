using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using MyWebPagesDownloader.Application.Options;
using Microsoft.Extensions.Options;
using System.Net;

namespace MyWebPagesDownloader.Application.Policies;

public sealed class RetryPolicyFactory
{
    private readonly RetryOptions _options;

    public RetryPolicyFactory(IOptions<RetryOptions> options)
    {
        _options = options.Value;
    }

    public IAsyncPolicy<T> CreateRetryPolicy<T>() where T : class
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .OrResult<T>(r => r == null)
            .WaitAndRetryAsync(
                retryCount: _options.MaxRetries,
                sleepDurationProvider: attempt =>
                {
                    var delay = Math.Min(
                        _options.InitialDelayMilliseconds * Math.Pow(_options.BackoffMultiplier, attempt - 1),
                        _options.MaxDelayMilliseconds);
                    return TimeSpan.FromMilliseconds(delay);
                });
    }

    public IAsyncPolicy<T> CreateCircuitBreakerPolicy<T>() where T : class
    {
        return Policy
            .Handle<HttpRequestException>()
            .OrResult<T>(r => r == null)
            .CircuitBreakerAsync<T>(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(10));
    }

    public IAsyncPolicy<T> CreateCombinedPolicy<T>() where T : class
    {
        var retry = CreateRetryPolicy<T>();
        var circuitBreaker = CreateCircuitBreakerPolicy<T>();
        return Policy.WrapAsync(retry, circuitBreaker);
    }
}

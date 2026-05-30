namespace MyWebPagesDownloader.Application.Options;

public sealed class RetryOptions
{
    public int MaxRetries { get; set; } = 3;
    public int InitialDelayMilliseconds { get; set; } = 100;
    public int MaxDelayMilliseconds { get; set; } = 5000;
    public double BackoffMultiplier { get; set; } = 2.0;
}

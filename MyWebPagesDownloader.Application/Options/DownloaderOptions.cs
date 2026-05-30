namespace MyWebPagesDownloader.Application.Options;

public sealed class DownloaderOptions
{
    public int MaxConcurrentWorkers { get; set; } = 5;
    public int ChannelCapacity { get; set; } = 1000;
    public int TimeoutSeconds { get; set; } = 30;
    public string OutputPath { get; set; } = "./downloads";
}

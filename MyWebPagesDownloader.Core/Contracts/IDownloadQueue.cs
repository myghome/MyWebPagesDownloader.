using MyWebPagesDownloader.Core.ValueObjects;

namespace MyWebPagesDownloader.Core.Contracts;

public interface IDownloadQueue
{
    ValueTask EnqueueAsync(
        DownloadRequest request,
        CancellationToken cancellationToken);

    IAsyncEnumerable<DownloadRequest> ReadAllAsync(
        CancellationToken cancellationToken);
}

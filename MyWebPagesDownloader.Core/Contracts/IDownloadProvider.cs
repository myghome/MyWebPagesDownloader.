using MyWebPagesDownloader.Core.ValueObjects;

namespace MyWebPagesDownloader.Core.Contracts;

public interface IDownloadProvider
{
    Task<DownloadResult> DownloadAsync(
        Uri url,
        CancellationToken cancellationToken);
}

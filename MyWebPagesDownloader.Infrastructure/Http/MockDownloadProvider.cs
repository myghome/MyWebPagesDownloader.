using MyWebPagesDownloader.Core.Contracts;
using MyWebPagesDownloader.Core.ValueObjects;
using System.Net;

namespace MyWebPagesDownloader.Infrastructure.Http;

public sealed class MockDownloadProvider : IDownloadProvider
{
    public async Task<DownloadResult> DownloadAsync(Uri url, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
        var content = $"Mock content for {url}";

        return new DownloadResult(
            Success: true,
            StatusCode: HttpStatusCode.OK,
            ContentLength: content.Length,
            Duration: TimeSpan.FromMilliseconds(100));
    }
}

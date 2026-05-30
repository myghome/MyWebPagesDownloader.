using System.Threading.Channels;
using MyWebPagesDownloader.Core.Contracts;
using MyWebPagesDownloader.Core.ValueObjects;

namespace MyWebPagesDownloader.Application.Queues;

public sealed class ChannelDownloadQueue : IDownloadQueue
{
    private readonly Channel<DownloadRequest> _channel;

    public ChannelDownloadQueue(int capacity = 1000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<DownloadRequest>(options);
    }

    public ValueTask EnqueueAsync(DownloadRequest request, CancellationToken cancellationToken)
    {
        return _channel.Writer.WriteAsync(request, cancellationToken);
    }

    public async IAsyncEnumerable<DownloadRequest> ReadAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var request in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return request;
        }
    }

    public void CompleteAdding()
    {
        _channel.Writer.TryComplete();
    }
}

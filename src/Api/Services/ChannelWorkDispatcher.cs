using System.Threading.Channels;

namespace Trace.Api.Services;

public class ChannelWorkDispatcher : IWorkDispatcher
{
    private readonly Channel<QueuedWorkReference> _channel = Channel.CreateBounded<QueuedWorkReference>(
        new BoundedChannelOptions(256)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

    public ValueTask QueueAsync(QueuedWorkReference work, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(work, ct);

    public IAsyncEnumerable<QueuedWorkReference> ReadAllAsync(CancellationToken ct = default) =>
        _channel.Reader.ReadAllAsync(ct);
}
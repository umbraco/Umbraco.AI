using System.Threading.Channels;

namespace Umbraco.Ai.Core.TaskQueue;

internal interface IBackgroundTaskQueue
{
    ValueTask QueueAsync(BackgroundWorkItem item, CancellationToken ct = default);
    ValueTask<BackgroundWorkItem> DequeueAsync(CancellationToken ct);
}

internal sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<BackgroundWorkItem> _channel;

    public BackgroundTaskQueue(int capacity = 1000)
    {
        _channel = Channel.CreateBounded<BackgroundWorkItem>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ValueTask QueueAsync(BackgroundWorkItem item, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(item, ct);

    public ValueTask<BackgroundWorkItem> DequeueAsync(CancellationToken ct)
        => _channel.Reader.ReadAsync(ct);
}

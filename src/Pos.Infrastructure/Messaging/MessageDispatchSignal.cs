using System.Threading.Channels;
using Pos.Application.Common.Abstractions.Services;

namespace Pos.Infrastructure.Messaging;

/// <summary>Collapses any number of wake-ups into one until the sender reads it.</summary>
internal sealed class MessageDispatchSignal : IMessageDispatchSignal
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    public void Notify() => _channel.Writer.TryWrite(true);

    public async Task WaitAsync(TimeSpan timeout, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);
        try
        {
            await _channel.Reader.ReadAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Timed out: check the queue anyway.
        }
    }
}

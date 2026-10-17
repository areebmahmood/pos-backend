using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pos.Application.Features.Messaging;

namespace Pos.Infrastructure.Messaging;

/// <summary>Background worker that sends queued WhatsApp messages without slowing down billing.</summary>
internal sealed class MessageDispatcher(
    IServiceScopeFactory scopeFactory,
    MessageDispatchSignal signal,
    IOptions<WhatsAppOptions> options,
    ILogger<MessageDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var poll = TimeSpan.FromSeconds(Math.Max(5, options.Value.PollSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                int sent;
                do
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    sent = await scope.ServiceProvider.GetRequiredService<IMessagingService>().DispatchPendingAsync(stoppingToken);
                } while (sent > 0 && !stoppingToken.IsCancellationRequested);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sending queued WhatsApp messages failed");
            }

            await signal.WaitAsync(poll, stoppingToken);
        }
    }
}

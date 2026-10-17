using Pos.Application.Common.Models;

namespace Pos.Application.Features.Messaging;

public interface IMessagingService
{
    /// <summary>Queues a promotional WhatsApp message for the chosen customers.</summary>
    Task<PromotionDto> SendPromotionAsync(SendPromotionRequest request, CancellationToken ct);

    Task<PagedResult<PromotionDto>> ListPromotionsAsync(PageRequest page, CancellationToken ct);

    Task<PagedResult<OutgoingMessageDto>> ListMessagesAsync(MessageQuery query, CancellationToken ct);

    /// <summary>Sends the queued messages that are due; returns how many were attempted.</summary>
    Task<int> DispatchPendingAsync(CancellationToken ct);
}

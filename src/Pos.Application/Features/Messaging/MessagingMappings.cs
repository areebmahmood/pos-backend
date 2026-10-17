using System.Linq.Expressions;
using Pos.Domain.Entities;
using Pos.Domain.Enums;

namespace Pos.Application.Features.Messaging;

public static class MessagingMappings
{
    public static readonly Expression<Func<OutgoingMessage, OutgoingMessageDto>> Projection = m => new OutgoingMessageDto(
        m.Id, m.Kind, m.Phone, m.Customer != null ? m.Customer.Name : null, m.Status, m.Error, m.Attempts,
        m.SaleId, m.PromotionId, m.CreatedAt, m.SentAt);

    public static readonly Expression<Func<Promotion, PromotionDto>> PromotionProjection = p => new PromotionDto(
        p.Id,
        p.Message,
        p.CreatedBy.FullName,
        p.CreatedAt,
        p.RecipientCount,
        p.Messages.Count(m => m.Status == MessageStatus.Sent),
        p.Messages.Count(m => m.Status == MessageStatus.Failed),
        p.Messages.Count(m => m.Status == MessageStatus.Pending));
}

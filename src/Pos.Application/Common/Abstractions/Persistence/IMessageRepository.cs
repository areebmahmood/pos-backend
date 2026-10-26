using System.Linq.Expressions;
using Pos.Application.Common.Models;
using Pos.Application.Features.Messaging;
using Pos.Domain.Entities;

namespace Pos.Application.Common.Abstractions.Persistence;

public interface IMessageRepository
{
    void Add(OutgoingMessage message);

    void AddPromotion(Promotion promotion);

    /// <summary>Unlinks the message history of a sale that is being deleted.</summary>
    Task DetachSaleAsync(int saleId, CancellationToken ct);

    /// <summary>The oldest pending messages that are due, tracked so their status can be updated.</summary>
    Task<IReadOnlyList<OutgoingMessage>> GetPendingAsync(int take, DateTime utcNow, CancellationToken ct);

    Task<PagedResult<TResult>> ListAsync<TResult>(
        MessageQuery query, Expression<Func<OutgoingMessage, TResult>> selector, CancellationToken ct);

    Task<PagedResult<TResult>> ListPromotionsAsync<TResult>(
        PageRequest page, Expression<Func<Promotion, TResult>> selector, CancellationToken ct);

    Task<TResult?> FindPromotionAsync<TResult>(int id, Expression<Func<Promotion, TResult>> selector, CancellationToken ct);
}

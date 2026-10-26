using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Models;
using Pos.Application.Features.Messaging;
using Pos.Domain.Entities;
using Pos.Domain.Enums;

namespace Pos.Infrastructure.Persistence.Repositories;

internal sealed class MessageRepository(PosDbContext db) : IMessageRepository
{
    public void Add(OutgoingMessage message) => db.OutgoingMessages.Add(message);

    public Task DetachSaleAsync(int saleId, CancellationToken ct) =>
        db.OutgoingMessages.Where(m => m.SaleId == saleId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.SaleId, (int?)null), ct);

    public void AddPromotion(Promotion promotion) => db.Promotions.Add(promotion);

    public async Task<IReadOnlyList<OutgoingMessage>> GetPendingAsync(int take, DateTime utcNow, CancellationToken ct) =>
        await db.OutgoingMessages
            .Where(m => m.Status == MessageStatus.Pending && (m.NextAttemptAt == null || m.NextAttemptAt <= utcNow))
            .OrderBy(m => m.Id)
            .Take(take)
            .ToListAsync(ct);

    public Task<PagedResult<TResult>> ListAsync<TResult>(
        MessageQuery query, Expression<Func<OutgoingMessage, TResult>> selector, CancellationToken ct) =>
        db.OutgoingMessages.AsNoTracking()
            .WhereIf(query.Kind is not null, m => m.Kind == query.Kind)
            .WhereIf(query.Status is not null, m => m.Status == query.Status)
            .WhereIf(query.PromotionId is not null, m => m.PromotionId == query.PromotionId)
            .OrderByDescending(m => m.Id)
            .ToPagedResultAsync(query, selector, ct);

    public Task<PagedResult<TResult>> ListPromotionsAsync<TResult>(
        PageRequest page, Expression<Func<Promotion, TResult>> selector, CancellationToken ct) =>
        db.Promotions.AsNoTracking()
            .OrderByDescending(p => p.Id)
            .ToPagedResultAsync(page, selector, ct);

    public Task<TResult?> FindPromotionAsync<TResult>(int id, Expression<Func<Promotion, TResult>> selector, CancellationToken ct) =>
        db.Promotions.AsNoTracking().Where(p => p.Id == id).Select(selector).FirstOrDefaultAsync(ct);
}

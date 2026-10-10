using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Models;
using Pos.Application.Features.Registers;
using Pos.Domain.Entities;
using Pos.Domain.Enums;

namespace Pos.Infrastructure.Persistence.Repositories;

internal sealed class RegisterSessionRepository(PosDbContext db) : Repository<RegisterSession>(db), IRegisterSessionRepository
{
    public Task<RegisterSession?> GetOpenForUserAsync(int userId, CancellationToken ct) =>
        Set.FirstOrDefaultAsync(r => r.UserId == userId && r.Status == RegisterStatus.Open, ct);

    public Task<bool> HasOpenForUserAsync(int userId, CancellationToken ct) =>
        Set.AnyAsync(r => r.UserId == userId && r.Status == RegisterStatus.Open, ct);

    public Task<TResult?> FindAsync<TResult>(int id, Expression<Func<RegisterSession, TResult>> selector, CancellationToken ct) =>
        Set.AsNoTracking().Where(r => r.Id == id).Select(selector).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<TResult>> ListOpenAsync<TResult>(
        Expression<Func<RegisterSession, TResult>> selector, CancellationToken ct) =>
        await Set.AsNoTracking()
            .Where(r => r.Status == RegisterStatus.Open)
            .OrderBy(r => r.OpenedAt)
            .Select(selector)
            .ToListAsync(ct);

    public Task<PagedResult<TResult>> ListAsync<TResult>(
        RegisterQuery query, Expression<Func<RegisterSession, TResult>> selector, CancellationToken ct) =>
        Set.AsNoTracking()
            .WhereIf(query.Status is not null, r => r.Status == query.Status)
            .WhereIf(query.UserId is not null, r => r.UserId == query.UserId)
            .OrderByDescending(r => r.OpenedAt)
            .ToPagedResultAsync(query, selector, ct);

    public async Task<IReadOnlyDictionary<int, RegisterTotals>> GetTotalsAsync(
        IReadOnlyCollection<int> registerIds, CancellationToken ct)
    {
        if (registerIds.Count == 0)
            return new Dictionary<int, RegisterTotals>();

        var sales = await Db.Sales.AsNoTracking()
            .Where(s => registerIds.Contains(s.RegisterSessionId))
            .GroupBy(s => s.RegisterSessionId)
            .Select(g => new
            {
                RegisterId = g.Key,
                Count = g.Count(),
                Cash = g.Where(s => s.PaymentMethod == PaymentMethod.Cash).Sum(s => (decimal?)s.Total) ?? 0,
                Card = g.Where(s => s.PaymentMethod == PaymentMethod.Card).Sum(s => (decimal?)s.Total) ?? 0
            })
            .ToDictionaryAsync(x => x.RegisterId, ct);

        var refunds = await Db.SaleReturns.AsNoTracking()
            .Where(r => registerIds.Contains(r.RegisterSessionId))
            .GroupBy(r => r.RegisterSessionId)
            .Select(g => new { RegisterId = g.Key, Amount = g.Sum(r => r.RefundAmount) })
            .ToDictionaryAsync(x => x.RegisterId, x => x.Amount, ct);

        var expenses = await Db.Expenses.AsNoTracking()
            .Where(e => registerIds.Contains(e.RegisterSessionId))
            .GroupBy(e => e.RegisterSessionId)
            .Select(g => new { RegisterId = g.Key, Amount = g.Sum(e => e.Amount) })
            .ToDictionaryAsync(x => x.RegisterId, x => x.Amount, ct);

        return registerIds.ToDictionary(id => id, id =>
        {
            var sale = sales.GetValueOrDefault(id);
            return new RegisterTotals(
                sale?.Count ?? 0,
                sale?.Cash ?? 0,
                sale?.Card ?? 0,
                refunds.GetValueOrDefault(id),
                expenses.GetValueOrDefault(id));
        });
    }
}

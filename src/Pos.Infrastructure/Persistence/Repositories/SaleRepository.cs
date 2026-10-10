using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Models;
using Pos.Application.Features.Sales;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Persistence.Repositories;

internal sealed class SaleRepository(PosDbContext db) : Repository<Sale>(db), ISaleRepository
{
    public Task<Sale?> GetWithItemsAsync(int id, CancellationToken ct) =>
        Set.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<TResult?> FindAsync<TResult>(int id, Expression<Func<Sale, TResult>> selector, CancellationToken ct) =>
        Set.AsNoTracking().Where(s => s.Id == id).Select(selector).FirstOrDefaultAsync(ct);

    public Task<TResult?> FindByInvoiceAsync<TResult>(string invoiceNumber, Expression<Func<Sale, TResult>> selector, CancellationToken ct) =>
        Set.AsNoTracking().Where(s => s.InvoiceNumber == invoiceNumber).Select(selector).FirstOrDefaultAsync(ct);

    public Task<PagedResult<TResult>> ListAsync<TResult>(
        SaleFilter filter, Expression<Func<Sale, TResult>> selector, CancellationToken ct)
    {
        var term = filter.Search.AsSearchTerm();
        var range = filter.Range;
        return Set.AsNoTracking()
            .WhereIf(term is not null, s =>
                s.InvoiceNumber.Contains(term!) ||
                (s.CustomerName != null && s.CustomerName.Contains(term!)) ||
                (s.CustomerPhone != null && s.CustomerPhone.Contains(term!)))
            .WhereIf(range is not null, s => s.CreatedAt >= range!.Value.FromUtc && s.CreatedAt < range.Value.ToUtc)
            .WhereIf(filter.CashierId is not null, s => s.CashierId == filter.CashierId)
            .OrderByDescending(s => s.CreatedAt)
            .ThenByDescending(s => s.Id)
            .ToPagedResultAsync(filter.Page, selector, ct);
    }

    public async Task<IReadOnlyList<TResult>> ListRecentAsync<TResult>(
        int? cashierId, int take, Expression<Func<Sale, TResult>> selector, CancellationToken ct) =>
        await Set.AsNoTracking()
            .WhereIf(cashierId is not null, s => s.CashierId == cashierId)
            .OrderByDescending(s => s.CreatedAt)
            .ThenByDescending(s => s.Id)
            .Take(take)
            .Select(selector)
            .ToListAsync(ct);
}

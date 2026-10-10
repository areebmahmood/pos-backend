using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Models;
using Pos.Application.Features.Returns;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Persistence.Repositories;

internal sealed class SaleReturnRepository(PosDbContext db) : ISaleReturnRepository
{
    public Task<TResult?> FindAsync<TResult>(int id, Expression<Func<SaleReturn, TResult>> selector, CancellationToken ct) =>
        db.SaleReturns.AsNoTracking().Where(r => r.Id == id).Select(selector).FirstOrDefaultAsync(ct);

    public Task<PagedResult<TResult>> ListAsync<TResult>(
        ReturnFilter filter, Expression<Func<SaleReturn, TResult>> selector, CancellationToken ct)
    {
        var term = filter.Search.AsSearchTerm();
        var range = filter.Range;
        return db.SaleReturns.AsNoTracking()
            .WhereIf(term is not null, r => r.Sale.InvoiceNumber.Contains(term!) || (r.Reason != null && r.Reason.Contains(term!)))
            .WhereIf(range is not null, r => r.CreatedAt >= range!.Value.FromUtc && r.CreatedAt < range.Value.ToUtc)
            .WhereIf(filter.ProcessedById is not null, r => r.ProcessedById == filter.ProcessedById)
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .ToPagedResultAsync(filter.Page, selector, ct);
    }
}

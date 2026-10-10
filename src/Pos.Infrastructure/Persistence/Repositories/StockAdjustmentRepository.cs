using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Models;
using Pos.Application.Features.Stock;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Persistence.Repositories;

internal sealed class StockAdjustmentRepository(PosDbContext db) : IStockAdjustmentRepository
{
    // The ledger stays complete for soft-deleted products, so product filters are bypassed here.
    private IQueryable<StockAdjustment> Query => db.StockAdjustments.AsNoTracking().IgnoreQueryFilters();

    public void Add(StockAdjustment adjustment) => db.StockAdjustments.Add(adjustment);

    public Task<TResult?> FindAsync<TResult>(int id, Expression<Func<StockAdjustment, TResult>> selector, CancellationToken ct) =>
        Query.Where(a => a.Id == id).Select(selector).FirstOrDefaultAsync(ct);

    public Task<PagedResult<TResult>> ListAsync<TResult>(
        StockAdjustmentQuery query, Expression<Func<StockAdjustment, TResult>> selector, CancellationToken ct) =>
        Query
            .WhereIf(query.ProductId is not null, a => a.ProductId == query.ProductId)
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .ToPagedResultAsync(query, selector, ct);
}

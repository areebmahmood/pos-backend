using System.Linq.Expressions;
using Pos.Application.Common.Models;
using Pos.Application.Features.Stock;
using Pos.Domain.Entities;

namespace Pos.Application.Common.Abstractions.Persistence;

public interface IStockAdjustmentRepository
{
    void Add(StockAdjustment adjustment);

    Task<TResult?> FindAsync<TResult>(int id, Expression<Func<StockAdjustment, TResult>> selector, CancellationToken ct);

    Task<PagedResult<TResult>> ListAsync<TResult>(
        StockAdjustmentQuery query, Expression<Func<StockAdjustment, TResult>> selector, CancellationToken ct);
}

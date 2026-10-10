using System.Linq.Expressions;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Stock;

public static class StockMappings
{
    public static readonly Expression<Func<StockAdjustment, StockAdjustmentDto>> Projection = a => new StockAdjustmentDto(
        a.Id,
        a.ProductId,
        a.Product.Name,
        a.Type,
        a.QuantityChange,
        a.QuantityBefore,
        a.QuantityAfter,
        a.Reason,
        a.User.FullName,
        a.CreatedAt);
}

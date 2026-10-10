using Pos.Application.Common.Models;
using Pos.Domain.Enums;

namespace Pos.Application.Features.Stock;

public sealed record StockAdjustmentDto(
    int Id,
    int ProductId,
    string ProductName,
    StockAdjustmentType Type,
    decimal QuantityChange,
    decimal QuantityBefore,
    decimal QuantityAfter,
    string? Reason,
    string UserName,
    DateTime CreatedAt);

public sealed record CreateStockAdjustmentRequest(int ProductId, decimal QuantityChange, string Reason);

public sealed record StockAdjustmentQuery : PageRequest
{
    public int? ProductId { get; init; }
}

using Pos.Application.Common.Models;

namespace Pos.Application.Features.Stock;

public interface IStockService
{
    Task<PagedResult<StockAdjustmentDto>> ListAsync(StockAdjustmentQuery query, CancellationToken ct);

    Task<StockAdjustmentDto> AdjustAsync(CreateStockAdjustmentRequest request, CancellationToken ct);
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Common.Models;
using Pos.Application.Features.Stock;
using Pos.Domain.Common;

namespace Pos.Api.Controllers;

[Route("api/stock-adjustments")]
public sealed class StockAdjustmentsController(IStockService stock) : ApiControllerBase
{
    [HttpGet]
    public Task<PagedResult<StockAdjustmentDto>> List([FromQuery] StockAdjustmentQuery query, CancellationToken ct) =>
        stock.ListAsync(query, ct);

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<StockAdjustmentDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateStockAdjustmentRequest request, CancellationToken ct)
    {
        var adjustment = await stock.AdjustAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, adjustment);
    }
}

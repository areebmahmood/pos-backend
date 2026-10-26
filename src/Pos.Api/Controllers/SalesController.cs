using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Common.Models;
using Pos.Application.Features.Sales;
using Pos.Domain.Common;

namespace Pos.Api.Controllers;

[Route("api/sales")]
public sealed class SalesController(ISaleService sales) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType<SaleDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateSaleRequest request, CancellationToken ct)
    {
        var sale = await sales.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = sale.Id }, sale);
    }

    [HttpGet]
    public Task<PagedResult<SaleSummaryDto>> List([FromQuery] SaleQuery query, CancellationToken ct) =>
        sales.ListAsync(query, ct);

    [HttpGet("{id:int}")]
    public Task<SaleDto> Get(int id, CancellationToken ct) => sales.GetAsync(id, ct);

    [HttpGet("invoice/{invoiceNumber}")]
    public Task<SaleDto> GetByInvoice(string invoiceNumber, CancellationToken ct) =>
        sales.GetByInvoiceAsync(invoiceNumber, ct);

    [HttpGet("{id:int}/receipt")]
    public Task<ReceiptDto> Receipt(int id, CancellationToken ct) => sales.GetReceiptAsync(id, ct);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await sales.DeleteAsync(id, ct);
        return NoContent();
    }
}

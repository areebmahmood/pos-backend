using Microsoft.AspNetCore.Mvc;
using Pos.Application.Common.Models;
using Pos.Application.Features.Returns;

namespace Pos.Api.Controllers;

[Route("api/returns")]
public sealed class ReturnsController(IReturnService returns) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType<SaleReturnDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateReturnRequest request, CancellationToken ct)
    {
        var saleReturn = await returns.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = saleReturn.Id }, saleReturn);
    }

    [HttpGet]
    public Task<PagedResult<SaleReturnDto>> List([FromQuery] ReturnQuery query, CancellationToken ct) =>
        returns.ListAsync(query, ct);

    [HttpGet("{id:int}")]
    public Task<SaleReturnDto> Get(int id, CancellationToken ct) => returns.GetAsync(id, ct);
}

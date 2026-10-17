using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Common.Models;
using Pos.Application.Features.DiscountCodes;
using Pos.Domain.Common;

namespace Pos.Api.Controllers;

[Route("api/discount-codes")]
public sealed class DiscountCodesController(IDiscountCodeService codes) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public Task<PagedResult<DiscountCodeDto>> List([FromQuery] DiscountCodeQuery query, CancellationToken ct) =>
        codes.ListAsync(query, ct);

    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public Task<DiscountCodeDto> Get(int id, CancellationToken ct) => codes.GetAsync(id, ct);

    /// <summary>Checks a code at billing (any signed-in user).</summary>
    [HttpGet("check")]
    public Task<DiscountCodeCheckDto> Check([FromQuery] string code, [FromQuery] decimal amount, CancellationToken ct) =>
        codes.CheckAsync(code, amount, ct);

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<DiscountCodeDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(SaveDiscountCodeRequest request, CancellationToken ct)
    {
        var code = await codes.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = code.Id }, code);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public Task<DiscountCodeDto> Update(int id, SaveDiscountCodeRequest request, CancellationToken ct) =>
        codes.UpdateAsync(id, request, ct);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await codes.DeleteAsync(id, ct);
        return NoContent();
    }
}

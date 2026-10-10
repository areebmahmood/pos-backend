using Microsoft.AspNetCore.Mvc;
using Pos.Application.Common.Models;
using Pos.Application.Features.Registers;

namespace Pos.Api.Controllers;

[Route("api/registers")]
public sealed class RegistersController(IRegisterService registers) : ApiControllerBase
{
    /// <summary>The caller's open register; 204 when none is open.</summary>
    [HttpGet("current")]
    [ProducesResponseType<RegisterSessionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Current(CancellationToken ct)
    {
        var register = await registers.GetCurrentAsync(ct);
        return register is null ? NoContent() : Ok(register);
    }

    [HttpGet]
    public Task<PagedResult<RegisterSessionDto>> List([FromQuery] RegisterQuery query, CancellationToken ct) =>
        registers.ListAsync(query, ct);

    [HttpGet("{id:int}")]
    public Task<RegisterSessionDto> Get(int id, CancellationToken ct) => registers.GetAsync(id, ct);

    [HttpPost("open")]
    [ProducesResponseType<RegisterSessionDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Open(OpenRegisterRequest request, CancellationToken ct)
    {
        var register = await registers.OpenAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = register.Id }, register);
    }

    [HttpPost("{id:int}/close")]
    public Task<RegisterSessionDto> Close(int id, CloseRegisterRequest request, CancellationToken ct) =>
        registers.CloseAsync(id, request, ct);
}

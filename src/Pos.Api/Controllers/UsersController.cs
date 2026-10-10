using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Common.Models;
using Pos.Application.Features.Users;
using Pos.Domain.Common;

namespace Pos.Api.Controllers;

[Route("api/users")]
[Authorize(Roles = Roles.Admin)]
public sealed class UsersController(IUserService users) : ApiControllerBase
{
    [HttpGet]
    public Task<PagedResult<UserDto>> List([FromQuery] UserQuery query, CancellationToken ct) => users.ListAsync(query, ct);

    [HttpGet("{id:int}")]
    public Task<UserDto> Get(int id, CancellationToken ct) => users.GetAsync(id, ct);

    [HttpPost]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken ct)
    {
        var user = await users.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpPut("{id:int}")]
    public Task<UserDto> Update(int id, UpdateUserRequest request, CancellationToken ct) => users.UpdateAsync(id, request, ct);

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await users.DeactivateAsync(id, ct);
        return NoContent();
    }
}

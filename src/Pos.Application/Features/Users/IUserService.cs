using Pos.Application.Common.Models;

namespace Pos.Application.Features.Users;

public interface IUserService
{
    Task<PagedResult<UserDto>> ListAsync(UserQuery query, CancellationToken ct);

    Task<UserDto> GetAsync(int id, CancellationToken ct);

    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct);

    Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct);

    Task DeactivateAsync(int id, CancellationToken ct);
}

using Pos.Application.Common.Models;
using Pos.Domain.Enums;

namespace Pos.Application.Features.Users;

public sealed record UserDto(int Id, string Username, string FullName, UserRole Role, bool IsActive, DateTime CreatedAt);

public sealed record CreateUserRequest(string Username, string FullName, string Password, UserRole Role);

public sealed record UpdateUserRequest(string FullName, UserRole Role, bool IsActive, string? Password);

public sealed record UserQuery : PageRequest
{
    public string? Search { get; init; }
}

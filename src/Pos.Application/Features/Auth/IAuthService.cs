using Pos.Application.Features.Users;

namespace Pos.Application.Features.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);

    Task<UserDto> GetCurrentUserAsync(CancellationToken ct);
}

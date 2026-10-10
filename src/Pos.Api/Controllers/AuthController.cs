using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pos.Api.Extensions;
using Pos.Application.Features.Auth;
using Pos.Application.Features.Users;

namespace Pos.Api.Controllers;

[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(ServiceCollectionExtensions.LoginRateLimitPolicy)]
    public Task<AuthResponse> Login(LoginRequest request, CancellationToken ct) =>
        authService.LoginAsync(request, ct);

    [HttpGet("me")]
    public Task<UserDto> Me(CancellationToken ct) => authService.GetCurrentUserAsync(ct);
}

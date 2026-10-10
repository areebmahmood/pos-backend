using Pos.Application.Features.Users;

namespace Pos.Application.Features.Auth;

public sealed record LoginRequest(string Username, string Password);

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, UserDto User);

using FluentValidation;
using Microsoft.Extensions.Logging;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Abstractions.Services;
using Pos.Application.Common.Exceptions;
using Pos.Application.Common.Validation;
using Pos.Application.Features.Users;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Auth;

public sealed class AuthService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    IValidator<LoginRequest> validator,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        await validator.EnsureValidAsync(request, ct);

        var user = await users.GetByUsernameAsync(User.NormalizeUsername(request.Username), ct);
        if (user is null || !user.IsActive || !passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            logger.LogWarning("Failed login attempt for {Username}", request.Username);
            throw new UnauthorizedException("Invalid username or password.");
        }

        user.RecordLogin(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);

        var (token, expiresAt) = tokenService.CreateAccessToken(user);
        return new AuthResponse(token, expiresAt, user.ToDto());
    }

    public async Task<UserDto> GetCurrentUserAsync(CancellationToken ct)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, ct);
        if (user is null || !user.IsActive)
            throw new UnauthorizedException("Your account is no longer active.");

        return user.ToDto();
    }
}

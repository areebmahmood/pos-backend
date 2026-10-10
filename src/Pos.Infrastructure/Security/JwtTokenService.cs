using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Pos.Application.Common.Abstractions.Services;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Security;

internal sealed class JwtTokenService(IOptions<JwtOptions> options, IDateTimeProvider clock) : ITokenService
{
    private readonly JwtOptions _options = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public (string Token, DateTime ExpiresAt) CreateAccessToken(User user)
    {
        var now = clock.UtcNow;
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            Subject = new ClaimsIdentity(
            [
                new Claim(PosClaimTypes.UserId, user.Id.ToString()),
                new Claim(PosClaimTypes.Username, user.Username),
                new Claim(PosClaimTypes.FullName, user.FullName),
                new Claim(PosClaimTypes.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            ]),
            SigningCredentials = new SigningCredentials(JwtSigningKey.Create(_options.SigningKey), SecurityAlgorithms.HmacSha256)
        };

        return (_handler.CreateToken(descriptor), expiresAt);
    }
}

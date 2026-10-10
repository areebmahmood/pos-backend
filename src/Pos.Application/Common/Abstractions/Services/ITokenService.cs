using Pos.Domain.Entities;

namespace Pos.Application.Common.Abstractions.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(User user);
}

using System.Security.Claims;
using Pos.Application.Common.Abstractions.Services;
using Pos.Application.Common.Exceptions;
using Pos.Domain.Enums;
using Pos.Infrastructure.Security;

namespace Pos.Api.Infrastructure;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public int UserId =>
        int.TryParse(Principal?.FindFirstValue(PosClaimTypes.UserId), out var id)
            ? id
            : throw new UnauthorizedException("You are not signed in.");

    public UserRole Role =>
        Enum.TryParse<UserRole>(Principal?.FindFirstValue(PosClaimTypes.Role), out var role)
            ? role
            : throw new UnauthorizedException("You are not signed in.");
}

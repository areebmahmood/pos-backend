using Pos.Domain.Enums;

namespace Pos.Application.Common.Abstractions.Services;

/// <summary>The authenticated caller of the current request.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>The user id; throws when the request is anonymous.</summary>
    int UserId { get; }

    UserRole Role { get; }

    bool IsAdmin => Role == UserRole.Admin;
}

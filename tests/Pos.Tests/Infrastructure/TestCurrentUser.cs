using Pos.Application.Common.Abstractions.Services;
using Pos.Domain.Enums;

namespace Pos.Tests.Infrastructure;

public sealed class TestCurrentUser : ICurrentUser
{
    public int? Id { get; set; }

    public UserRole CurrentRole { get; set; } = UserRole.Admin;

    public bool IsAuthenticated => Id is not null;

    public int UserId => Id ?? throw new InvalidOperationException("No user signed in.");

    public UserRole Role => CurrentRole;
}

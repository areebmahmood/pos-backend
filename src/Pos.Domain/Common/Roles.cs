using Pos.Domain.Enums;

namespace Pos.Domain.Common;

/// <summary>Role names as they appear in authorization attributes and tokens.</summary>
public static class Roles
{
    public const string Admin = nameof(UserRole.Admin);
    public const string Salesman = nameof(UserRole.Salesman);
}

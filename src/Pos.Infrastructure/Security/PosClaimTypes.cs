namespace Pos.Infrastructure.Security;

/// <summary>Short JWT claim names used in issued tokens.</summary>
public static class PosClaimTypes
{
    public const string UserId = "sub";
    public const string Username = "unique_name";
    public const string FullName = "name";
    public const string Role = "role";
}

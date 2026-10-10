using System.ComponentModel.DataAnnotations;

namespace Pos.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = default!;

    [Required]
    public string Audience { get; init; } = default!;

    /// <summary>HMAC-SHA256 signing key; must be at least 32 characters. Keep it out of source control.</summary>
    [Required, MinLength(32)]
    public string SigningKey { get; init; } = default!;

    [Range(5, 24 * 60)]
    public int AccessTokenMinutes { get; init; } = 720;
}

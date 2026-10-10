using Pos.Domain.Common;
using Pos.Domain.Enums;

namespace Pos.Domain.Entities;

public class User : AuditableEntity
{
    public const int UsernameMaxLength = 50;
    public const int FullNameMaxLength = 100;

    private User() { }

    public User(string username, string fullName, UserRole role, string passwordHash)
    {
        Username = NormalizeUsername(username);
        FullName = Guard.NotEmpty(fullName, "Full name", FullNameMaxLength);
        Role = role;
        PasswordHash = passwordHash;
        IsActive = true;
    }

    public string Username { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    public static string NormalizeUsername(string username) =>
        Guard.NotEmpty(username, "Username", UsernameMaxLength).ToLowerInvariant();

    public void Update(string fullName, UserRole role, bool isActive)
    {
        FullName = Guard.NotEmpty(fullName, "Full name", FullNameMaxLength);
        Role = role;
        IsActive = isActive;
    }

    public void ChangePasswordHash(string passwordHash) => PasswordHash = passwordHash;

    public void Deactivate() => IsActive = false;

    public void RecordLogin(DateTime utcNow) => LastLoginAt = utcNow;
}

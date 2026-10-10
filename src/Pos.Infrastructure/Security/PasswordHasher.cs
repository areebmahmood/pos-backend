using Microsoft.AspNetCore.Identity;
using Pos.Domain.Entities;
using IPasswordHasher = Pos.Application.Common.Abstractions.Services.IPasswordHasher;

namespace Pos.Infrastructure.Security;

/// <summary>PBKDF2 hashing via ASP.NET Core Identity's battle-tested implementation.</summary>
internal sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    public string Hash(string password) => _inner.HashPassword(null!, password);

    public bool Verify(string hash, string password) =>
        _inner.VerifyHashedPassword(null!, hash, password) != PasswordVerificationResult.Failed;
}

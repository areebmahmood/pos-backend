using Pos.Application.Common.Abstractions.Persistence;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;

namespace Pos.Application.Features.Registers;

public static class RegisterRepositoryExtensions
{
    /// <summary>Returns the caller's open register or fails with a business-rule error.</summary>
    public static async Task<RegisterSession> RequireOpenForUserAsync(
        this IRegisterSessionRepository registers, int userId, string action, CancellationToken ct) =>
        await registers.GetOpenForUserAsync(userId, ct)
        ?? throw new DomainException($"Open your register before {action}.");
}

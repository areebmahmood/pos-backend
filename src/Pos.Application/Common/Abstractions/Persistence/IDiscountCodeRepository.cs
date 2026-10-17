using System.Linq.Expressions;
using Pos.Application.Common.Models;
using Pos.Domain.Entities;

namespace Pos.Application.Common.Abstractions.Persistence;

public interface IDiscountCodeRepository : IRepository<DiscountCode>
{
    /// <summary>Loads a code (tracked) by its normalized text.</summary>
    Task<DiscountCode?> GetByCodeAsync(string code, CancellationToken ct);

    Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken ct);

    Task<TResult?> FindAsync<TResult>(int id, Expression<Func<DiscountCode, TResult>> selector, CancellationToken ct);

    Task<PagedResult<TResult>> ListAsync<TResult>(
        PageRequest page, string? search, Expression<Func<DiscountCode, TResult>> selector, CancellationToken ct);
}

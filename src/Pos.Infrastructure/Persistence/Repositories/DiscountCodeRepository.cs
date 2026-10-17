using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Models;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Persistence.Repositories;

internal sealed class DiscountCodeRepository(PosDbContext db) : Repository<DiscountCode>(db), IDiscountCodeRepository
{
    public Task<DiscountCode?> GetByCodeAsync(string code, CancellationToken ct) =>
        Set.FirstOrDefaultAsync(d => d.Code == code, ct);

    public Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken ct) =>
        Set.AnyAsync(d => d.Code == code && (excludeId == null || d.Id != excludeId), ct);

    public Task<TResult?> FindAsync<TResult>(int id, Expression<Func<DiscountCode, TResult>> selector, CancellationToken ct) =>
        Set.AsNoTracking().Where(d => d.Id == id).Select(selector).FirstOrDefaultAsync(ct);

    public Task<PagedResult<TResult>> ListAsync<TResult>(
        PageRequest page, string? search, Expression<Func<DiscountCode, TResult>> selector, CancellationToken ct)
    {
        var term = search.AsSearchTerm();
        return Set.AsNoTracking()
            .WhereIf(term is not null, d => d.Code.Contains(term!) || (d.Description != null && d.Description.Contains(term!)))
            .OrderByDescending(d => d.IsActive)
            .ThenByDescending(d => d.Id)
            .ToPagedResultAsync(page, selector, ct);
    }
}

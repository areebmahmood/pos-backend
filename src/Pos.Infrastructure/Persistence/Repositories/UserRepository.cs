using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Models;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(PosDbContext db) : Repository<User>(db), IUserRepository
{
    public Task<User?> GetByUsernameAsync(string normalizedUsername, CancellationToken ct) =>
        Set.FirstOrDefaultAsync(u => u.Username == normalizedUsername, ct);

    public Task<bool> UsernameExistsAsync(string normalizedUsername, CancellationToken ct) =>
        Set.AnyAsync(u => u.Username == normalizedUsername, ct);

    public Task<PagedResult<TResult>> ListAsync<TResult>(
        string? search, PageRequest page, Expression<Func<User, TResult>> selector, CancellationToken ct)
    {
        var term = search.AsSearchTerm();
        return Set.AsNoTracking()
            .WhereIf(term is not null, u => u.Username.Contains(term!) || u.FullName.Contains(term!))
            .OrderBy(u => u.FullName)
            .ToPagedResultAsync(page, selector, ct);
    }
}

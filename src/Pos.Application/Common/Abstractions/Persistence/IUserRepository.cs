using System.Linq.Expressions;
using Pos.Application.Common.Models;
using Pos.Domain.Entities;

namespace Pos.Application.Common.Abstractions.Persistence;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string normalizedUsername, CancellationToken ct);

    Task<bool> UsernameExistsAsync(string normalizedUsername, CancellationToken ct);

    Task<PagedResult<TResult>> ListAsync<TResult>(
        string? search, PageRequest page, Expression<Func<User, TResult>> selector, CancellationToken ct);
}

using System.Linq.Expressions;
using Pos.Domain.Entities;

namespace Pos.Application.Common.Abstractions.Persistence;

public interface ICategoryRepository : IRepository<Category>
{
    Task<IReadOnlyList<TResult>> ListAsync<TResult>(Expression<Func<Category, TResult>> selector, CancellationToken ct);

    Task<TResult?> FindAsync<TResult>(int id, Expression<Func<Category, TResult>> selector, CancellationToken ct);

    Task<bool> ExistsAsync(int id, CancellationToken ct);

    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct);

    Task<bool> HasProductsAsync(int id, CancellationToken ct);
}

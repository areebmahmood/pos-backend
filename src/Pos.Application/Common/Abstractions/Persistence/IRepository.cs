using Pos.Domain.Common;

namespace Pos.Application.Common.Abstractions.Persistence;

/// <summary>Write-side repository for an aggregate root. Changes are committed through <see cref="IUnitOfWork"/>.</summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct);

    void Add(T entity);

    void Remove(T entity);
}

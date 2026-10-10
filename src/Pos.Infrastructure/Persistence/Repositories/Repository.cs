using Microsoft.EntityFrameworkCore;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Domain.Common;

namespace Pos.Infrastructure.Persistence.Repositories;

internal abstract class Repository<T>(PosDbContext db) : IRepository<T> where T : BaseEntity
{
    protected PosDbContext Db { get; } = db;

    protected DbSet<T> Set => Db.Set<T>();

    public virtual Task<T?> GetByIdAsync(int id, CancellationToken ct) =>
        Set.FirstOrDefaultAsync(e => e.Id == id, ct);

    public void Add(T entity) => Set.Add(entity);

    public void Remove(T entity) => Set.Remove(entity);
}

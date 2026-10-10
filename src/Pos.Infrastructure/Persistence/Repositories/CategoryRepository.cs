using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository(PosDbContext db) : Repository<Category>(db), ICategoryRepository
{
    public async Task<IReadOnlyList<TResult>> ListAsync<TResult>(Expression<Func<Category, TResult>> selector, CancellationToken ct) =>
        await Set.AsNoTracking().OrderBy(c => c.Name).Select(selector).ToListAsync(ct);

    public Task<TResult?> FindAsync<TResult>(int id, Expression<Func<Category, TResult>> selector, CancellationToken ct) =>
        Set.AsNoTracking().Where(c => c.Id == id).Select(selector).FirstOrDefaultAsync(ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct) => Set.AnyAsync(c => c.Id == id, ct);

    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct) =>
        Set.AnyAsync(c => c.Name == name && (excludeId == null || c.Id != excludeId), ct);

    public Task<bool> HasProductsAsync(int id, CancellationToken ct) =>
        Db.Products.AnyAsync(p => p.CategoryId == id, ct);
}

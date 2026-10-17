using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Models;
using Pos.Application.Features.Customers;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Persistence.Repositories;

internal sealed class CustomerRepository(PosDbContext db) : Repository<Customer>(db), ICustomerRepository
{
    public Task<Customer?> GetByPhoneAsync(string phone, CancellationToken ct) =>
        Set.FirstOrDefaultAsync(c => c.Phone == phone, ct);

    public Task<bool> PhoneExistsAsync(string phone, int? excludeId, CancellationToken ct) =>
        Set.AnyAsync(c => c.Phone == phone && (excludeId == null || c.Id != excludeId), ct);

    public Task<TResult?> FindAsync<TResult>(int id, Expression<Func<Customer, TResult>> selector, CancellationToken ct) =>
        Set.AsNoTracking().Where(c => c.Id == id).Select(selector).FirstOrDefaultAsync(ct);

    public Task<PagedResult<TResult>> ListAsync<TResult>(
        CustomerFilter filter, Expression<Func<Customer, TResult>> selector, CancellationToken ct)
    {
        var term = filter.Search.AsSearchTerm();
        return Set.AsNoTracking()
            .WhereIf(term is not null, c => c.Phone.Contains(term!) || (c.Name != null && c.Name.Contains(term!)))
            .WhereIf(filter.OptedOut is not null, c => c.OptedOut == filter.OptedOut)
            .OrderByDescending(c => c.Sales.Max(s => (DateTime?)s.CreatedAt) ?? c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .ToPagedResultAsync(filter.Page, selector, ct);
    }

    public async Task<IReadOnlyList<Customer>> ListPromotionRecipientsAsync(IReadOnlyCollection<int>? ids, CancellationToken ct) =>
        await Set.AsNoTracking()
            .Where(c => !c.OptedOut)
            .WhereIf(ids is not null, c => ids!.Contains(c.Id))
            .OrderBy(c => c.Id)
            .ToListAsync(ct);
}

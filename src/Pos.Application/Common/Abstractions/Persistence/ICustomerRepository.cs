using System.Linq.Expressions;
using Pos.Application.Common.Models;
using Pos.Application.Features.Customers;
using Pos.Domain.Entities;

namespace Pos.Application.Common.Abstractions.Persistence;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByPhoneAsync(string phone, CancellationToken ct);

    Task<bool> PhoneExistsAsync(string phone, int? excludeId, CancellationToken ct);

    Task<TResult?> FindAsync<TResult>(int id, Expression<Func<Customer, TResult>> selector, CancellationToken ct);

    Task<PagedResult<TResult>> ListAsync<TResult>(
        CustomerFilter filter, Expression<Func<Customer, TResult>> selector, CancellationToken ct);

    /// <summary>Customers who accept promotions; all of them when <paramref name="ids"/> is null.</summary>
    Task<IReadOnlyList<Customer>> ListPromotionRecipientsAsync(IReadOnlyCollection<int>? ids, CancellationToken ct);
}

using System.Linq.Expressions;
using Pos.Application.Common.Models;
using Pos.Application.Features.Sales;
using Pos.Domain.Entities;

namespace Pos.Application.Common.Abstractions.Persistence;

public interface ISaleRepository : IRepository<Sale>
{
    /// <summary>Loads a sale with its items (tracked, for returns).</summary>
    Task<Sale?> GetWithItemsAsync(int id, CancellationToken ct);

    /// <summary>Loads a sale with its items, products and returns (tracked, for deletion).</summary>
    Task<Sale?> GetForDeletionAsync(int id, CancellationToken ct);

    /// <summary>Removes a sale together with its returns.</summary>
    void RemoveWithReturns(Sale sale);

    Task<TResult?> FindAsync<TResult>(int id, Expression<Func<Sale, TResult>> selector, CancellationToken ct);

    Task<TResult?> FindByInvoiceAsync<TResult>(string invoiceNumber, Expression<Func<Sale, TResult>> selector, CancellationToken ct);

    Task<PagedResult<TResult>> ListAsync<TResult>(
        SaleFilter filter, Expression<Func<Sale, TResult>> selector, CancellationToken ct);

    Task<IReadOnlyList<TResult>> ListRecentAsync<TResult>(
        int? cashierId, int take, Expression<Func<Sale, TResult>> selector, CancellationToken ct);
}

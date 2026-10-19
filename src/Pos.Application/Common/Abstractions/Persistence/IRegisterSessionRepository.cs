using System.Linq.Expressions;
using Pos.Application.Common.Models;
using Pos.Application.Features.Registers;
using Pos.Domain.Entities;

namespace Pos.Application.Common.Abstractions.Persistence;

public interface IRegisterSessionRepository : IRepository<RegisterSession>
{
    Task<RegisterSession?> GetOpenForUserAsync(int userId, CancellationToken ct);

    Task<bool> HasOpenForUserAsync(int userId, CancellationToken ct);

    Task<TResult?> FindAsync<TResult>(int id, Expression<Func<RegisterSession, TResult>> selector, CancellationToken ct);

    Task<IReadOnlyList<TResult>> ListOpenAsync<TResult>(Expression<Func<RegisterSession, TResult>> selector, CancellationToken ct);

    Task<PagedResult<TResult>> ListAsync<TResult>(
        RegisterQuery query, Expression<Func<RegisterSession, TResult>> selector, CancellationToken ct);

    /// <summary>Cash movement totals for each of the given registers.</summary>
    Task<IReadOnlyDictionary<int, RegisterTotals>> GetTotalsAsync(IReadOnlyCollection<int> registerIds, CancellationToken ct);
}

public sealed record RegisterTotals(
    int SaleCount,
    decimal CashSales,
    decimal CardSales,
    decimal OnlineSales,
    decimal CashRefunds,
    decimal Expenses)
{
    public static readonly RegisterTotals Empty = new(0, 0, 0, 0, 0, 0);
}

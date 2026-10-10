using System.Linq.Expressions;
using Pos.Application.Common.Models;
using Pos.Application.Features.Expenses;
using Pos.Domain.Entities;

namespace Pos.Application.Common.Abstractions.Persistence;

public interface IExpenseRepository : IRepository<Expense>
{
    Task<TResult?> FindAsync<TResult>(int id, Expression<Func<Expense, TResult>> selector, CancellationToken ct);

    Task<PagedResult<TResult>> ListAsync<TResult>(
        ExpenseFilter filter, Expression<Func<Expense, TResult>> selector, CancellationToken ct);
}

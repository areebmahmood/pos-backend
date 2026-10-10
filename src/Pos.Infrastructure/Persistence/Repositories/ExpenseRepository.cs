using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Models;
using Pos.Application.Features.Expenses;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Persistence.Repositories;

internal sealed class ExpenseRepository(PosDbContext db) : Repository<Expense>(db), IExpenseRepository
{
    public Task<TResult?> FindAsync<TResult>(int id, Expression<Func<Expense, TResult>> selector, CancellationToken ct) =>
        Set.AsNoTracking().Where(e => e.Id == id).Select(selector).FirstOrDefaultAsync(ct);

    public Task<PagedResult<TResult>> ListAsync<TResult>(
        ExpenseFilter filter, Expression<Func<Expense, TResult>> selector, CancellationToken ct)
    {
        var term = filter.Search.AsSearchTerm();
        var range = filter.Range;
        return Set.AsNoTracking()
            .WhereIf(term is not null, e => e.Title.Contains(term!) || (e.Category != null && e.Category.Contains(term!)))
            .WhereIf(range is not null, e => e.CreatedAt >= range!.Value.FromUtc && e.CreatedAt < range.Value.ToUtc)
            .WhereIf(filter.CreatedById is not null, e => e.CreatedById == filter.CreatedById)
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .ToPagedResultAsync(filter.Page, selector, ct);
    }
}

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Pos.Application.Common.Models;

namespace Pos.Infrastructure.Persistence.Repositories;

internal static class QueryableExtensions
{
    public static async Task<PagedResult<TResult>> ToPagedResultAsync<TSource, TResult>(
        this IQueryable<TSource> query,
        PageRequest page,
        Expression<Func<TSource, TResult>> selector,
        CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var items = total == 0
            ? []
            : await query.Skip(page.Skip).Take(page.PageSize).Select(selector).ToListAsync(ct);

        return new PagedResult<TResult>(items, page.Page, page.PageSize, total);
    }

    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate) =>
        condition ? query.Where(predicate) : query;

    /// <summary>Normalizes a free-text search term; returns <c>null</c> when there is nothing to search for.</summary>
    public static string? AsSearchTerm(this string? search) =>
        string.IsNullOrWhiteSpace(search) ? null : search.Trim();
}

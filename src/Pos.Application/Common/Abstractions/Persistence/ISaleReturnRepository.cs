using System.Linq.Expressions;
using Pos.Application.Common.Models;
using Pos.Application.Features.Returns;
using Pos.Domain.Entities;

namespace Pos.Application.Common.Abstractions.Persistence;

public interface ISaleReturnRepository
{
    Task<TResult?> FindAsync<TResult>(int id, Expression<Func<SaleReturn, TResult>> selector, CancellationToken ct);

    Task<PagedResult<TResult>> ListAsync<TResult>(
        ReturnFilter filter, Expression<Func<SaleReturn, TResult>> selector, CancellationToken ct);
}

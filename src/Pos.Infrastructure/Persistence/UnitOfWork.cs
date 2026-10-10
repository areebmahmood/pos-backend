using Microsoft.EntityFrameworkCore;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Exceptions;

namespace Pos.Infrastructure.Persistence;

internal sealed class UnitOfWork(PosDbContext db) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            return await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The data was changed by another user (for example, stock was sold at another counter). Please retry.");
        }
    }

    public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        // If a transaction is already active, join it rather than nesting.
        if (db.Database.CurrentTransaction is not null)
            return action(ct);

        var strategy = db.Database.CreateExecutionStrategy();
        var attempt = 0;
        return strategy.ExecuteAsync(async token =>
        {
            // A retried attempt must start from a clean slate, so changes from the failed attempt are discarded.
            if (attempt++ > 0)
                db.ChangeTracker.Clear();

            await using var transaction = await db.Database.BeginTransactionAsync(token);
            var result = await action(token);
            await transaction.CommitAsync(token);
            return result;
        }, ct);
    }
}

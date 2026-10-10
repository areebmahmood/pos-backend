namespace Pos.Application.Common.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);

    /// <summary>
    /// Runs <paramref name="action"/> inside a database transaction (with the provider's retry strategy).
    /// The transaction commits when the action completes and rolls back if it throws.
    /// On a transient failure the whole action is retried with an empty change tracker, so the action
    /// must load every entity it modifies itself.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct);
}

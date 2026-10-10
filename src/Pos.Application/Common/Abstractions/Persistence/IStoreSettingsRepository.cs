using Pos.Domain.Entities;

namespace Pos.Application.Common.Abstractions.Persistence;

public interface IStoreSettingsRepository
{
    /// <summary>Returns the settings row, creating it with defaults on first use.</summary>
    Task<StoreSettings> GetAsync(CancellationToken ct);
}

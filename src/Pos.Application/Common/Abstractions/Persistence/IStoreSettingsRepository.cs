using Pos.Domain.Entities;

namespace Pos.Application.Common.Abstractions.Persistence;

public interface IStoreSettingsRepository
{
    /// <summary>Returns the settings row, creating it with defaults on first use.</summary>
    Task<StoreSettings> GetAsync(CancellationToken ct);

    Task<StoreImage?> GetImageAsync(string kind, CancellationToken ct);

    /// <summary>When each uploaded image was last replaced, by kind, without loading the bytes.</summary>
    Task<IReadOnlyDictionary<string, DateTime>> GetImageVersionsAsync(CancellationToken ct);

    void AddImage(StoreImage image);

    void RemoveImage(StoreImage image);
}

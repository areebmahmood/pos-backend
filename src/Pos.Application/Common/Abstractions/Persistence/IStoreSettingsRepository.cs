using Pos.Domain.Entities;

namespace Pos.Application.Common.Abstractions.Persistence;

public interface IStoreSettingsRepository
{
    /// <summary>Returns the settings row, creating it with defaults on first use.</summary>
    Task<StoreSettings> GetAsync(CancellationToken ct);

    Task<StoreImage?> GetImageAsync(string kind, CancellationToken ct);

    /// <summary>When the image was last replaced, without loading its bytes; <c>null</c> if there is none.</summary>
    Task<DateTime?> GetImageUpdatedAtAsync(string kind, CancellationToken ct);

    void AddImage(StoreImage image);

    void RemoveImage(StoreImage image);
}

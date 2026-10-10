namespace Pos.Application.Features.Settings;

public interface ISettingsService
{
    Task<StoreSettingsDto> GetAsync(CancellationToken ct);

    Task<StoreSettingsDto> UpdateAsync(UpdateStoreSettingsRequest request, CancellationToken ct);
}

namespace Pos.Application.Features.Settings;

public interface ISettingsService
{
    Task<StoreSettingsDto> GetAsync(CancellationToken ct);

    Task<StoreSettingsDto> UpdateAsync(UpdateStoreSettingsRequest request, CancellationToken ct);

    /// <param name="kind">One of <see cref="Pos.Domain.Entities.StoreImage.Kinds"/>.</param>
    Task<StoreImageFile> GetImageAsync(string kind, CancellationToken ct);

    Task<StoreSettingsDto> SetImageAsync(string kind, byte[] content, CancellationToken ct);

    Task<StoreSettingsDto> RemoveImageAsync(string kind, CancellationToken ct);

    Task<WhatsAppSettingsDto> GetWhatsAppAsync(CancellationToken ct);

    Task<WhatsAppSettingsDto> UpdateWhatsAppAsync(UpdateWhatsAppSettingsRequest request, CancellationToken ct);

    /// <summary>Sends one test message right away using the saved settings.</summary>
    Task<WhatsAppTestResult> SendWhatsAppTestAsync(WhatsAppTestRequest request, CancellationToken ct);
}

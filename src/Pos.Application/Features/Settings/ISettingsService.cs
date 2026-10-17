namespace Pos.Application.Features.Settings;

public interface ISettingsService
{
    Task<StoreSettingsDto> GetAsync(CancellationToken ct);

    Task<StoreSettingsDto> UpdateAsync(UpdateStoreSettingsRequest request, CancellationToken ct);

    Task<StoreImageFile> GetWhatsAppGroupQrAsync(CancellationToken ct);

    Task<StoreSettingsDto> SetWhatsAppGroupQrAsync(byte[] content, CancellationToken ct);

    Task<StoreSettingsDto> RemoveWhatsAppGroupQrAsync(CancellationToken ct);

    Task<WhatsAppSettingsDto> GetWhatsAppAsync(CancellationToken ct);

    Task<WhatsAppSettingsDto> UpdateWhatsAppAsync(UpdateWhatsAppSettingsRequest request, CancellationToken ct);

    /// <summary>Sends one test message right away using the saved settings.</summary>
    Task<WhatsAppTestResult> SendWhatsAppTestAsync(WhatsAppTestRequest request, CancellationToken ct);
}

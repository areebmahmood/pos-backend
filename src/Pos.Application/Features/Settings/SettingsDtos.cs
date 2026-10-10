namespace Pos.Application.Features.Settings;

public sealed record StoreSettingsDto(
    string StoreName,
    string? Address,
    string? Phone,
    string? ReceiptFooter,
    string? UrduInstructions,
    string? ReceiptQrBaseUrl,
    string CurrencySymbol,
    string TimeZoneId);

public sealed record UpdateStoreSettingsRequest(
    string StoreName,
    string? Address,
    string? Phone,
    string? ReceiptFooter,
    string? UrduInstructions,
    string? ReceiptQrBaseUrl,
    string CurrencySymbol);

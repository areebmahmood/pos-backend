namespace Pos.Application.Features.Settings;

public sealed record StoreSettingsDto(
    string StoreName,
    string? Address,
    string? Phone,
    string? ReceiptFooter,
    string? UrduInstructions,
    string? WhatsAppGroupCaption,
    DateTime? WhatsAppGroupQrUpdatedAt,
    string CurrencySymbol,
    string TimeZoneId);

public sealed record UpdateStoreSettingsRequest(
    string StoreName,
    string? Address,
    string? Phone,
    string? ReceiptFooter,
    string? UrduInstructions,
    string? WhatsAppGroupCaption,
    string CurrencySymbol);

public sealed record StoreImageFile(byte[] Content, string ContentType);

/// <summary>WhatsApp Business Cloud API settings. The access token is never sent back, only whether one is saved.</summary>
public sealed record WhatsAppSettingsDto(
    bool Enabled,
    string? PhoneNumberId,
    bool HasAccessToken,
    string? ThankYouTemplate,
    string? PromotionTemplate,
    string TemplateLanguage,
    string DefaultCountryCode);

/// <param name="AccessToken">A new token; omit or leave empty to keep the saved one.</param>
public sealed record UpdateWhatsAppSettingsRequest(
    bool Enabled,
    string? PhoneNumberId,
    string? AccessToken,
    string? ThankYouTemplate,
    string? PromotionTemplate,
    string TemplateLanguage,
    string DefaultCountryCode);

public sealed record WhatsAppTestRequest(string Phone);

public sealed record WhatsAppTestResult(bool Success, string Template, string? Error);

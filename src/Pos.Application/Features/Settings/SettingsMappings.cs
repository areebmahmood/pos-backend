using Pos.Domain.Entities;

namespace Pos.Application.Features.Settings;

public static class SettingsMappings
{
    /// <param name="imageVersions">When each uploaded store image was last replaced, by <see cref="StoreImage"/> kind.</param>
    public static StoreSettingsDto ToDto(
        this StoreSettings s, string timeZoneId, IReadOnlyDictionary<string, DateTime> imageVersions) => new(
        s.StoreName, s.Address, s.Phone, s.ReceiptFooter, s.UrduInstructions, s.WhatsAppGroupCaption,
        Version(imageVersions, StoreImage.WhatsAppGroupQr), Version(imageVersions, StoreImage.StoreLogo),
        Version(imageVersions, StoreImage.ReceiptLogo), s.CurrencySymbol, timeZoneId);

    private static DateTime? Version(IReadOnlyDictionary<string, DateTime> versions, string kind) =>
        versions.TryGetValue(kind, out var updatedAt) ? updatedAt : null;

    public static WhatsAppSettingsDto ToWhatsAppDto(this StoreSettings s) => new(
        s.WhatsAppEnabled, s.WhatsAppPhoneNumberId, s.WhatsAppAccessToken is not null, s.WhatsAppThankYouTemplate,
        s.WhatsAppPromotionTemplate, s.WhatsAppTemplateLanguage, s.DefaultCountryCode);
}

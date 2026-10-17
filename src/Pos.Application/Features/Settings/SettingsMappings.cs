using Pos.Domain.Entities;

namespace Pos.Application.Features.Settings;

public static class SettingsMappings
{
    public static StoreSettingsDto ToDto(this StoreSettings s, string timeZoneId, DateTime? whatsAppGroupQrUpdatedAt) => new(
        s.StoreName, s.Address, s.Phone, s.ReceiptFooter, s.UrduInstructions, s.WhatsAppGroupCaption,
        whatsAppGroupQrUpdatedAt, s.CurrencySymbol, timeZoneId);

    public static WhatsAppSettingsDto ToWhatsAppDto(this StoreSettings s) => new(
        s.WhatsAppEnabled, s.WhatsAppPhoneNumberId, s.WhatsAppAccessToken is not null, s.WhatsAppThankYouTemplate,
        s.WhatsAppPromotionTemplate, s.WhatsAppTemplateLanguage, s.DefaultCountryCode);
}

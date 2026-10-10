using Pos.Domain.Entities;

namespace Pos.Application.Features.Settings;

public static class SettingsMappings
{
    public static StoreSettingsDto ToDto(this StoreSettings s, string timeZoneId) => new(
        s.StoreName, s.Address, s.Phone, s.ReceiptFooter, s.UrduInstructions, s.ReceiptQrBaseUrl, s.CurrencySymbol, timeZoneId);
}

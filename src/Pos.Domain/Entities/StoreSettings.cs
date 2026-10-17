using Pos.Domain.Common;

namespace Pos.Domain.Entities;

/// <summary>Single-row table holding store details printed on receipts.</summary>
public class StoreSettings : AuditableEntity
{
    public const int SingletonId = 1;

    public const string DefaultUrduInstructions =
        "خریدا ہوا سامان رسید کے ساتھ 7 دن کے اندر تبدیل یا واپس ہو سکتا ہے۔\n" +
        "استعمال شدہ یا خراب شدہ اشیاء واپس نہیں ہوں گی۔\n" +
        "براہ کرم کاؤنٹر چھوڑنے سے پہلے اپنا سامان اور بقایا رقم چیک کر لیں۔\n" +
        "خریداری کا شکریہ! دوبارہ تشریف لائیں۔";

    private StoreSettings() { }

    public static StoreSettings CreateDefault() => new()
    {
        Id = SingletonId,
        StoreName = "My Store",
        CurrencySymbol = "Rs",
        ReceiptFooter = "Thank you for shopping with us!",
        UrduInstructions = DefaultUrduInstructions,
        WhatsAppGroupCaption = DefaultWhatsAppGroupCaption,
        DefaultCountryCode = "92",
        WhatsAppTemplateLanguage = "en"
    };

    public const string DefaultWhatsAppGroupCaption = "Scan to join our WhatsApp group for offers";

    public string StoreName { get; private set; } = default!;
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public string? ReceiptFooter { get; private set; }
    public string? UrduInstructions { get; private set; }

    /// <summary>Printed under the WhatsApp group QR code on receipts.</summary>
    public string? WhatsAppGroupCaption { get; private set; }

    public string CurrencySymbol { get; private set; } = default!;

    /// <summary>Country calling code added to local phone numbers (e.g. 92 for Pakistan).</summary>
    public string DefaultCountryCode { get; private set; } = "92";

    // WhatsApp Business Cloud API (Meta). Messages are only sent when enabled and fully configured.
    public bool WhatsAppEnabled { get; private set; }
    public string? WhatsAppPhoneNumberId { get; private set; }
    public string? WhatsAppAccessToken { get; private set; }
    public string? WhatsAppThankYouTemplate { get; private set; }
    public string? WhatsAppPromotionTemplate { get; private set; }
    public string WhatsAppTemplateLanguage { get; private set; } = "en";

    public bool WhatsAppConfigured =>
        WhatsAppEnabled && WhatsAppPhoneNumberId is not null && WhatsAppAccessToken is not null;

    public void Update(
        string storeName,
        string? address,
        string? phone,
        string? receiptFooter,
        string? urduInstructions,
        string? whatsAppGroupCaption,
        string currencySymbol)
    {
        StoreName = Guard.NotEmpty(storeName, "Store name", 150);
        Address = Guard.Optional(address, "Address", 300);
        Phone = Guard.Optional(phone, "Phone", 50);
        ReceiptFooter = Guard.Optional(receiptFooter, "Receipt footer", 500);
        UrduInstructions = Guard.Optional(urduInstructions, "Urdu instructions", 2000);
        WhatsAppGroupCaption = Guard.Optional(whatsAppGroupCaption, "WhatsApp group caption", 200);
        CurrencySymbol = Guard.NotEmpty(currencySymbol, "Currency symbol", 10);
    }

    /// <param name="accessToken">A new token, or <c>null</c> to keep the saved one.</param>
    public void UpdateWhatsApp(
        bool enabled,
        string? phoneNumberId,
        string? accessToken,
        string? thankYouTemplate,
        string? promotionTemplate,
        string templateLanguage,
        string defaultCountryCode)
    {
        WhatsAppPhoneNumberId = Guard.Optional(phoneNumberId, "Phone number ID", 50);
        if (!string.IsNullOrWhiteSpace(accessToken))
            WhatsAppAccessToken = Guard.Optional(accessToken, "Access token", 1000);
        WhatsAppThankYouTemplate = Guard.Optional(thankYouTemplate, "Thank-you template", OutgoingMessage.TemplateMaxLength);
        WhatsAppPromotionTemplate = Guard.Optional(promotionTemplate, "Promotion template", OutgoingMessage.TemplateMaxLength);
        WhatsAppTemplateLanguage = Guard.NotEmpty(templateLanguage, "Template language", OutgoingMessage.LanguageMaxLength);

        var code = Guard.NotEmpty(defaultCountryCode, "Country code", 5).TrimStart('+');
        if (code.Length == 0 || !code.All(char.IsAsciiDigit))
            throw new Exceptions.DomainException("Country code must be digits, e.g. 92.");
        DefaultCountryCode = code;

        if (enabled && (WhatsAppPhoneNumberId is null || WhatsAppAccessToken is null))
            throw new Exceptions.DomainException("Enter the phone number ID and access token before turning WhatsApp on.");
        WhatsAppEnabled = enabled;
    }

    public void ClearWhatsAppAccessToken()
    {
        WhatsAppAccessToken = null;
        WhatsAppEnabled = false;
    }
}

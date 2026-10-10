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
        UrduInstructions = DefaultUrduInstructions
    };

    public string StoreName { get; private set; } = default!;
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public string? ReceiptFooter { get; private set; }
    public string? UrduInstructions { get; private set; }

    /// <summary>When set, the receipt QR code links to this URL followed by the invoice number.</summary>
    public string? ReceiptQrBaseUrl { get; private set; }

    public string CurrencySymbol { get; private set; } = default!;

    public void Update(
        string storeName,
        string? address,
        string? phone,
        string? receiptFooter,
        string? urduInstructions,
        string? receiptQrBaseUrl,
        string currencySymbol)
    {
        StoreName = Guard.NotEmpty(storeName, "Store name", 150);
        Address = Guard.Optional(address, "Address", 300);
        Phone = Guard.Optional(phone, "Phone", 50);
        ReceiptFooter = Guard.Optional(receiptFooter, "Receipt footer", 500);
        UrduInstructions = Guard.Optional(urduInstructions, "Urdu instructions", 2000);
        ReceiptQrBaseUrl = Guard.Optional(receiptQrBaseUrl, "QR base URL", 300);
        CurrencySymbol = Guard.NotEmpty(currencySymbol, "Currency symbol", 10);
    }
}

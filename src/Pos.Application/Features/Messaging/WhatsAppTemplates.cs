using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Messaging;

/// <summary>
/// Builds the body parameters for the store's WhatsApp templates. The templates are created in
/// Meta's WhatsApp Manager and must have exactly these variables:
/// thank-you: {{1}} customer name, {{2}} invoice number, {{3}} bill total;
/// promotion: {{1}} customer name, {{2}} the promotion text.
/// </summary>
public static partial class WhatsAppTemplates
{
    public const string FallbackName = "Customer";

    /// <summary>Meta's sample template that every new WhatsApp Business account has; used for test messages.</summary>
    public const string HelloWorld = "hello_world";

    public static string[] ThankYou(string? customerName, string invoiceNumber, decimal total, string currencySymbol) =>
    [
        Clean(customerName, FallbackName),
        invoiceNumber,
        $"{currencySymbol} {total.ToString("#,0.##", CultureInfo.InvariantCulture)}"
    ];

    public static string[] Promotion(string? customerName, string message) =>
        [Clean(customerName, FallbackName), Clean(message, "")];

    public static string Serialize(IReadOnlyList<string> parameters) => JsonSerializer.Serialize(parameters);

    public static IReadOnlyList<string> Deserialize(string json) =>
        JsonSerializer.Deserialize<string[]>(json) ?? [];

    /// <summary>WhatsApp rejects parameters with new lines, tabs or more than four spaces in a row.</summary>
    public static string Clean(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;
        return Whitespace().Replace(value.Trim(), " ");
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    public static OutgoingMessage? ForSale(StoreSettings settings, Sale sale, DateTime utcNow)
    {
        if (!settings.WhatsAppConfigured || settings.WhatsAppThankYouTemplate is null || sale.Customer is null)
            return null;

        var parameters = ThankYou(sale.CustomerName ?? sale.Customer.Name, sale.InvoiceNumber, sale.Total, settings.CurrencySymbol);
        return new OutgoingMessage(
            Domain.Enums.MessageKind.ThankYou, sale.Customer.Phone, settings.WhatsAppThankYouTemplate,
            settings.WhatsAppTemplateLanguage, Serialize(parameters), utcNow, sale.Customer.Id, sale.Id);
    }
}

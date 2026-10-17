namespace Pos.Application.Common.Abstractions.Services;

/// <summary>Credentials for the WhatsApp Business Cloud API, taken from store settings.</summary>
public sealed record WhatsAppAccount(string PhoneNumberId, string AccessToken);

/// <param name="Retryable">The failure was temporary (rate limit, network, server error) and the message can be tried again.</param>
public sealed record WhatsAppSendResult(bool Success, string? Error = null, bool Retryable = false)
{
    public static readonly WhatsAppSendResult Sent = new(true);
}

/// <summary>Sends approved WhatsApp template messages.</summary>
public interface IWhatsAppClient
{
    /// <param name="phone">International digits without the plus, e.g. 923001234567.</param>
    Task<WhatsAppSendResult> SendTemplateAsync(
        WhatsAppAccount account, string phone, string templateName, string language,
        IReadOnlyList<string> parameters, CancellationToken ct);
}

/// <summary>Wakes the background sender when new messages are queued.</summary>
public interface IMessageDispatchSignal
{
    void Notify();
}

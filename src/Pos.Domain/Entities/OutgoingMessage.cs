using Pos.Domain.Enums;

namespace Pos.Domain.Entities;

/// <summary>
/// A WhatsApp template message waiting to be sent, or already sent. Messages are queued in the
/// database so a restart never loses them, and a background worker sends the pending ones.
/// </summary>
public class OutgoingMessage
{
    public const int TemplateMaxLength = 100;
    public const int LanguageMaxLength = 15;
    public const int ParametersMaxLength = 2000;
    public const int ErrorMaxLength = 500;
    public const int MaxAttempts = 3;

    private OutgoingMessage() { }

    public OutgoingMessage(
        MessageKind kind, string phone, string templateName, string language, string parameters,
        DateTime utcNow, int? customerId = null, int? saleId = null, int? promotionId = null)
    {
        Kind = kind;
        Phone = phone;
        TemplateName = templateName;
        Language = language;
        Parameters = parameters;
        CustomerId = customerId;
        SaleId = saleId;
        PromotionId = promotionId;
        Status = MessageStatus.Pending;
        CreatedAt = utcNow;
    }

    public int Id { get; private set; }
    public MessageKind Kind { get; private set; }
    public string Phone { get; private set; } = default!;
    public string TemplateName { get; private set; } = default!;
    public string Language { get; private set; } = default!;

    /// <summary>JSON array of the template's body parameters, in order.</summary>
    public string Parameters { get; private set; } = default!;

    public int? CustomerId { get; private set; }
    public Customer? Customer { get; private set; }
    public int? SaleId { get; private set; }
    public int? PromotionId { get; private set; }
    public MessageStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public string? Error { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }

    /// <summary>A pending message that failed for a temporary reason waits until this time before the next try.</summary>
    public DateTime? NextAttemptAt { get; private set; }

    public void MarkSent(DateTime utcNow)
    {
        Attempts++;
        Status = MessageStatus.Sent;
        Error = null;
        SentAt = utcNow;
        NextAttemptAt = null;
    }

    /// <summary>Records a failed attempt; a retryable failure stays pending until <see cref="MaxAttempts"/>.</summary>
    public void MarkFailed(string error, bool retryable, DateTime utcNow)
    {
        Attempts++;
        Error = error.Length > ErrorMaxLength ? error[..ErrorMaxLength] : error;
        Status = retryable && Attempts < MaxAttempts ? MessageStatus.Pending : MessageStatus.Failed;
        NextAttemptAt = Status == MessageStatus.Pending ? utcNow.AddMinutes(2 * Attempts) : null;
    }
}

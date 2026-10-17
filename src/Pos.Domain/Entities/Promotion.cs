using Pos.Domain.Common;

namespace Pos.Domain.Entities;

/// <summary>A bulk promotional WhatsApp message sent to a set of customers.</summary>
public class Promotion : AuditableEntity
{
    public const int MessageMaxLength = 1000;

    private readonly List<OutgoingMessage> _messages = [];

    private Promotion() { }

    public Promotion(string message, int createdById, int recipientCount)
    {
        Message = Guard.NotEmpty(message, "Message", MessageMaxLength);
        CreatedById = createdById;
        RecipientCount = recipientCount;
    }

    public string Message { get; private set; } = default!;
    public int CreatedById { get; private set; }
    public User CreatedBy { get; private set; } = default!;
    public int RecipientCount { get; private set; }
    public IReadOnlyCollection<OutgoingMessage> Messages => _messages;
}

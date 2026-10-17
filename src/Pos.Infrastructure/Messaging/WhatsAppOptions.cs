namespace Pos.Infrastructure.Messaging;

public sealed class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    /// <summary>Meta Graph API base address including the version.</summary>
    public string GraphApiBaseUrl { get; init; } = "https://graph.facebook.com/v23.0/";

    /// <summary>How often the sender checks for queued messages when nothing wakes it.</summary>
    public int PollSeconds { get; init; } = 30;
}

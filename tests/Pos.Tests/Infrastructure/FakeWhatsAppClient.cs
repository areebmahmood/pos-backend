using Pos.Application.Common.Abstractions.Services;

namespace Pos.Tests.Infrastructure;

/// <summary>Records template messages instead of calling Meta.</summary>
public sealed class FakeWhatsAppClient : IWhatsAppClient
{
    public List<(string Phone, string Template, IReadOnlyList<string> Parameters)> Sent { get; } = [];

    /// <summary>When set, every send fails with this result.</summary>
    public WhatsAppSendResult? FailWith { get; set; }

    public Task<WhatsAppSendResult> SendTemplateAsync(
        WhatsAppAccount account, string phone, string templateName, string language,
        IReadOnlyList<string> parameters, CancellationToken ct)
    {
        if (FailWith is not null)
            return Task.FromResult(FailWith);

        Sent.Add((phone, templateName, parameters));
        return Task.FromResult(WhatsAppSendResult.Sent);
    }
}

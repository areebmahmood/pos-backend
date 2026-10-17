using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Pos.Application.Common.Abstractions.Services;

namespace Pos.Infrastructure.Messaging;

/// <summary>Sends template messages through the WhatsApp Business Cloud API (Meta).</summary>
internal sealed class WhatsAppCloudClient(HttpClient http) : IWhatsAppClient
{
    public async Task<WhatsAppSendResult> SendTemplateAsync(
        WhatsAppAccount account, string phone, string templateName, string language,
        IReadOnlyList<string> parameters, CancellationToken ct)
    {
        var template = new Dictionary<string, object>
        {
            ["name"] = templateName,
            ["language"] = new { code = language }
        };
        if (parameters.Count > 0)
        {
            template["components"] = new object[]
            {
                new { type = "body", parameters = parameters.Select(p => new { type = "text", text = p }).ToArray() }
            };
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{Uri.EscapeDataString(account.PhoneNumberId)}/messages")
        {
            Content = JsonContent.Create(new
            {
                messaging_product = "whatsapp",
                to = phone,
                type = "template",
                template
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", account.AccessToken);

        try
        {
            using var response = await http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
                return WhatsAppSendResult.Sent;

            var body = await response.Content.ReadAsStringAsync(ct);
            var retryable = response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;
            return new WhatsAppSendResult(false, DescribeError(response.StatusCode, body), retryable);
        }
        catch (HttpRequestException ex)
        {
            return new WhatsAppSendResult(false, $"Could not reach WhatsApp: {ex.Message}", Retryable: true);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new WhatsAppSendResult(false, "WhatsApp did not answer in time.", Retryable: true);
        }
    }

    /// <summary>Pulls Meta's error message (and details, which name the problem) out of the response.</summary>
    private static string DescribeError(HttpStatusCode status, string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("error", out var error))
            {
                var message = error.TryGetProperty("message", out var m) ? m.GetString() : null;
                var code = error.TryGetProperty("code", out var c) ? c.ToString() : null;
                var details = error.TryGetProperty("error_data", out var data) && data.TryGetProperty("details", out var d)
                    ? d.GetString()
                    : null;
                return $"WhatsApp error {code}: {message}{(details is null ? "" : $" ({details})")}";
            }
        }
        catch (JsonException)
        {
            // Not JSON; fall through.
        }

        return $"WhatsApp returned {(int)status} {status}.";
    }
}

using Pos.Application.Common.Models;
using Pos.Domain.Enums;

namespace Pos.Application.Features.Messaging;

public sealed record OutgoingMessageDto(
    int Id,
    MessageKind Kind,
    string Phone,
    string? CustomerName,
    MessageStatus Status,
    string? Error,
    int Attempts,
    int? SaleId,
    int? PromotionId,
    DateTime CreatedAt,
    DateTime? SentAt);

public sealed record MessageQuery : PageRequest
{
    public MessageKind? Kind { get; init; }
    public MessageStatus? Status { get; init; }
    public int? PromotionId { get; init; }
}

public sealed record PromotionDto(
    int Id,
    string Message,
    string CreatedBy,
    DateTime CreatedAt,
    int RecipientCount,
    int SentCount,
    int FailedCount,
    int PendingCount);

/// <param name="CustomerIds">Customers to message; omit or leave empty for every customer who accepts promotions.</param>
public sealed record SendPromotionRequest(string Message, IReadOnlyList<int>? CustomerIds);

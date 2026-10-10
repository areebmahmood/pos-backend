using Pos.Application.Common.Models;

namespace Pos.Application.Features.Returns;

public sealed record SaleReturnItemDto(int SaleItemId, int ProductId, string ProductName, decimal Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record SaleReturnDto(
    int Id,
    int SaleId,
    string InvoiceNumber,
    decimal RefundAmount,
    string? Reason,
    string ProcessedBy,
    DateTime CreatedAt,
    IReadOnlyList<SaleReturnItemDto> Items);

public sealed record ReturnLineRequest(int SaleItemId, decimal Quantity);

public sealed record CreateReturnRequest(int SaleId, string? Reason, IReadOnlyList<ReturnLineRequest> Items);

public sealed record ReturnQuery : PageRequest
{
    public string? Search { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
}

public sealed record ReturnFilter(PageRequest Page, string? Search, DateRange? Range, int? ProcessedById);

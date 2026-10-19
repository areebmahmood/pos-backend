using Pos.Application.Common.Models;
using Pos.Application.Features.Settings;
using Pos.Domain.Enums;

namespace Pos.Application.Features.Sales;

public sealed record SaleItemDto(
    int Id,
    int ProductId,
    string ProductName,
    string Barcode,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountedUnitPrice,
    decimal UnitDiscount,
    decimal ManualDiscount,
    decimal LineDiscount,
    decimal LineTotal,
    decimal ReturnedQuantity);

public sealed record SaleDto(
    int Id,
    string InvoiceNumber,
    int RegisterSessionId,
    string CashierName,
    string? CustomerName,
    string? CustomerPhone,
    int? CustomerId,
    PaymentMethod PaymentMethod,
    OnlinePaymentType? OnlinePaymentType,
    decimal Subtotal,
    decimal ItemDiscount,
    decimal BillDiscount,
    string? DiscountCode,
    decimal CodeDiscount,
    decimal Total,
    decimal AmountPaid,
    decimal Change,
    decimal ReturnedAmount,
    SaleStatus Status,
    DateTime CreatedAt,
    IReadOnlyList<SaleItemDto> Items);

public sealed record SaleSummaryDto(
    int Id,
    string InvoiceNumber,
    string CashierName,
    string? CustomerName,
    PaymentMethod PaymentMethod,
    OnlinePaymentType? OnlinePaymentType,
    decimal Total,
    int ItemCount,
    SaleStatus Status,
    DateTime CreatedAt);

public sealed record ReceiptDto(StoreSettingsDto Store, SaleDto Sale, string QrContent);

/// <param name="Discount">Optional extra discount per unit, on top of the product's discounted price; it is multiplied by the quantity.</param>
public sealed record SaleLineRequest(int ProductId, decimal Quantity, decimal Discount = 0);

public sealed record CreateSaleRequest(
    IReadOnlyList<SaleLineRequest> Items,
    PaymentMethod PaymentMethod,
    decimal AmountPaid,
    decimal BillDiscount,
    string? CustomerName,
    string? CustomerPhone,
    string? DiscountCode = null,
    OnlinePaymentType? OnlinePaymentType = null);

public sealed record SaleQuery : PageRequest
{
    public string? Search { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public int? CashierId { get; init; }
}

public sealed record SaleFilter(PageRequest Page, string? Search, DateRange? Range, int? CashierId);

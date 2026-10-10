using System.Linq.Expressions;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Sales;

public static class SaleMappings
{
    public static readonly Expression<Func<Sale, SaleDto>> Projection = s => new SaleDto(
        s.Id,
        s.InvoiceNumber,
        s.RegisterSessionId,
        s.Cashier.FullName,
        s.CustomerName,
        s.CustomerPhone,
        s.PaymentMethod,
        s.Subtotal,
        s.ItemDiscount,
        s.BillDiscount,
        s.Total,
        s.AmountPaid,
        s.Change,
        s.ReturnedAmount,
        s.Status,
        s.CreatedAt,
        s.Items
            .OrderBy(i => i.Id)
            .Select(i => new SaleItemDto(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.Barcode,
                i.Quantity,
                i.UnitPrice,
                i.DiscountedUnitPrice,
                i.LineDiscount,
                i.LineTotal,
                i.ReturnedQuantity))
            .ToList());

    public static readonly Expression<Func<Sale, SaleSummaryDto>> SummaryProjection = s => new SaleSummaryDto(
        s.Id,
        s.InvoiceNumber,
        s.Cashier.FullName,
        s.CustomerName,
        s.PaymentMethod,
        s.Total,
        s.Items.Count(),
        s.Status,
        s.CreatedAt);
}

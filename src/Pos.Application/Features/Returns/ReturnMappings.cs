using System.Linq.Expressions;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Returns;

public static class ReturnMappings
{
    public static readonly Expression<Func<SaleReturn, SaleReturnDto>> Projection = r => new SaleReturnDto(
        r.Id,
        r.SaleId,
        r.Sale.InvoiceNumber,
        r.RefundAmount,
        r.Reason,
        r.ProcessedBy.FullName,
        r.CreatedAt,
        r.Items
            .OrderBy(i => i.Id)
            .Select(i => new SaleReturnItemDto(i.SaleItemId, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.LineTotal))
            .ToList());
}

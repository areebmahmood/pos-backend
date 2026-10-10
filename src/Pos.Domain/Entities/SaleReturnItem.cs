using Pos.Domain.Common;

namespace Pos.Domain.Entities;

public class SaleReturnItem : BaseEntity
{
    private SaleReturnItem() { }

    internal SaleReturnItem(SaleItem saleItem, decimal quantity, decimal paidUnitPrice)
    {
        SaleItem = saleItem;
        SaleItemId = saleItem.Id;
        ProductId = saleItem.ProductId;
        ProductName = saleItem.ProductName;
        Quantity = quantity;
        UnitPrice = Money.Round(paidUnitPrice);
        UnitCost = saleItem.UnitCost;
        LineTotal = Money.Round(paidUnitPrice * quantity);
    }

    public int SaleReturnId { get; private set; }
    public int SaleItemId { get; private set; }
    public SaleItem SaleItem { get; private set; } = default!;
    public int ProductId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public decimal Quantity { get; private set; }

    /// <summary>Refunded amount per unit.</summary>
    public decimal UnitPrice { get; private set; }

    public decimal UnitCost { get; private set; }
    public decimal LineTotal { get; private set; }
}

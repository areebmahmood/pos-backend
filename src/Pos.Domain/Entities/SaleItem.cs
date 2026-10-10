using Pos.Domain.Common;
using Pos.Domain.Exceptions;

namespace Pos.Domain.Entities;

/// <summary>
/// A sold line. Product name, barcode, prices and cost are snapshotted at the moment of sale.
/// </summary>
public class SaleItem : BaseEntity
{
    private SaleItem() { }

    internal SaleItem(Product product, decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException($"Quantity for '{product.Name}' must be greater than zero.");

        Product = product;
        ProductId = product.Id;
        ProductName = product.Name;
        Barcode = product.Barcode;
        Quantity = quantity;
        UnitPrice = product.SalePrice;
        DiscountedUnitPrice = product.EffectivePrice;
        UnitCost = product.CostPrice;
        LineDiscount = Money.Round((UnitPrice - DiscountedUnitPrice) * quantity);
        LineTotal = Money.Round(DiscountedUnitPrice * quantity);
    }

    public int SaleId { get; private set; }
    public int ProductId { get; private set; }
    public Product Product { get; private set; } = default!;
    public string ProductName { get; private set; } = default!;
    public string Barcode { get; private set; } = default!;
    public decimal Quantity { get; private set; }

    /// <summary>List price per unit at the time of sale.</summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>Price per unit after the product discount.</summary>
    public decimal DiscountedUnitPrice { get; private set; }

    public decimal UnitCost { get; private set; }
    public decimal LineDiscount { get; private set; }
    public decimal LineTotal { get; private set; }
    public decimal ReturnedQuantity { get; private set; }

    public decimal ReturnableQuantity => Quantity - ReturnedQuantity;

    internal void Merge(decimal quantity)
    {
        Quantity += quantity;
        LineDiscount = Money.Round((UnitPrice - DiscountedUnitPrice) * Quantity);
        LineTotal = Money.Round(DiscountedUnitPrice * Quantity);
    }

    internal void RegisterReturn(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Return quantity must be greater than zero.");
        if (quantity > ReturnableQuantity)
            throw new DomainException($"Cannot return more than {ReturnableQuantity:0.###} of '{ProductName}'.");

        ReturnedQuantity += quantity;
    }
}

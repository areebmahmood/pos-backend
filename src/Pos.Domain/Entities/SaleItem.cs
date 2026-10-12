using Pos.Domain.Common;
using Pos.Domain.Exceptions;

namespace Pos.Domain.Entities;

/// <summary>
/// A sold line. Product name, barcode, prices and cost are snapshotted at the moment of sale.
/// </summary>
public class SaleItem : BaseEntity
{
    private SaleItem() { }

    internal SaleItem(Product product, decimal quantity, decimal unitDiscount)
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
        Recalculate(DiscountFor(unitDiscount, quantity));
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

    /// <summary>Extra discount per unit the cashier gave, on top of the product's discounted price.</summary>
    public decimal UnitDiscount { get; private set; }

    /// <summary>The cashier's extra discount for the whole line (<see cref="UnitDiscount"/> × quantity).</summary>
    public decimal ManualDiscount { get; private set; }

    /// <summary>Total discount on the line: product discount plus <see cref="ManualDiscount"/>.</summary>
    public decimal LineDiscount { get; private set; }
    public decimal LineTotal { get; private set; }
    public decimal ReturnedQuantity { get; private set; }

    public decimal ReturnableQuantity => Quantity - ReturnedQuantity;

    /// <summary>Price actually charged per unit after all line-level discounts.</summary>
    public decimal NetUnitPrice => Quantity == 0 ? 0 : LineTotal / Quantity;

    internal void Merge(decimal quantity, decimal unitDiscount)
    {
        var addedDiscount = DiscountFor(unitDiscount, quantity);
        Quantity += quantity;
        Recalculate(ManualDiscount + addedDiscount);
    }

    private decimal DiscountFor(decimal unitDiscount, decimal quantity)
    {
        Guard.NotNegative(unitDiscount, "Discount");
        if (unitDiscount > DiscountedUnitPrice)
            throw new DomainException($"Discount on '{ProductName}' cannot exceed its price ({DiscountedUnitPrice:0.00}).");

        return Money.Round(unitDiscount * quantity);
    }

    private void Recalculate(decimal manualDiscount)
    {
        var discountedTotal = Money.Round(DiscountedUnitPrice * Quantity);
        manualDiscount = Math.Min(manualDiscount, discountedTotal);

        ManualDiscount = manualDiscount;
        UnitDiscount = Math.Round(manualDiscount / Quantity, 4, MidpointRounding.AwayFromZero);
        LineDiscount = Money.Round((UnitPrice - DiscountedUnitPrice) * Quantity + manualDiscount);
        LineTotal = discountedTotal - manualDiscount;
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

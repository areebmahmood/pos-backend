using Pos.Domain.Common;
using Pos.Domain.Exceptions;

namespace Pos.Domain.Entities;

public class Product : AuditableEntity, ISoftDeletable
{
    public const int NameMaxLength = 200;
    public const int BarcodeMaxLength = 64;
    public const int DescriptionMaxLength = 2000;

    private Product() { }

    public Product(
        string name,
        string barcode,
        string? description,
        int? categoryId,
        decimal costPrice,
        decimal salePrice,
        decimal? discountedPrice,
        decimal reorderLevel)
    {
        UpdateDetails(name, barcode, description, categoryId, costPrice, salePrice, discountedPrice, reorderLevel, isActive: true);
        ConcurrencyStamp = Guid.NewGuid();
    }

    public string Name { get; private set; } = default!;
    public string Barcode { get; private set; } = default!;
    public string? Description { get; private set; }
    public int? CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public decimal CostPrice { get; private set; }
    public decimal SalePrice { get; private set; }
    public decimal? DiscountedPrice { get; private set; }
    public decimal StockQuantity { get; private set; }
    public decimal ReorderLevel { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    /// <summary>Optimistic concurrency token; regenerated on every stock change.</summary>
    public Guid ConcurrencyStamp { get; private set; }

    /// <summary>The unit price a customer is charged.</summary>
    public decimal EffectivePrice => DiscountedPrice ?? SalePrice;

    public bool IsLowStock => StockQuantity <= ReorderLevel;

    public void UpdateDetails(
        string name,
        string barcode,
        string? description,
        int? categoryId,
        decimal costPrice,
        decimal salePrice,
        decimal? discountedPrice,
        decimal reorderLevel,
        bool isActive)
    {
        Name = Guard.NotEmpty(name, "Product name", NameMaxLength);
        Barcode = Guard.NotEmpty(barcode, "Barcode", BarcodeMaxLength);
        Description = Guard.Optional(description, "Description", DescriptionMaxLength);
        CategoryId = categoryId;
        CostPrice = Guard.NotNegative(costPrice, "Cost price");
        SalePrice = Guard.Positive(salePrice, "Sale price");
        ReorderLevel = Guard.NotNegative(reorderLevel, "Reorder level");
        IsActive = isActive;
        SetDiscount(discountedPrice);
    }

    public void SetDiscount(decimal? discountedPrice)
    {
        if (discountedPrice is { } price)
        {
            Guard.NotNegative(price, "Discounted price");
            if (price >= SalePrice)
                throw new DomainException("Discounted price must be lower than the sale price.");
        }

        DiscountedPrice = discountedPrice;
    }

    /// <summary>Applies a signed stock change and returns the quantity before the change.</summary>
    public decimal AdjustStock(decimal quantityChange)
    {
        if (quantityChange == 0)
            throw new DomainException("Quantity change cannot be zero.");

        var before = StockQuantity;
        var after = before + quantityChange;
        if (after < 0)
            throw new DomainException($"Insufficient stock for '{Name}'. Available: {before:0.###}.");

        StockQuantity = after;
        ConcurrencyStamp = Guid.NewGuid();
        return before;
    }

    public void Delete(DateTime utcNow)
    {
        IsDeleted = true;
        IsActive = false;
        DeletedAt = utcNow;
    }
}

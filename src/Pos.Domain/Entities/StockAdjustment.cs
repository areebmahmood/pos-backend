using Pos.Domain.Common;
using Pos.Domain.Enums;

namespace Pos.Domain.Entities;

/// <summary>Immutable ledger entry recording every change to a product's stock.</summary>
public class StockAdjustment : AuditableEntity
{
    public const int ReasonMaxLength = 500;

    private StockAdjustment() { }

    public StockAdjustment(
        Product product,
        StockAdjustmentType type,
        decimal quantityChange,
        decimal quantityBefore,
        int userId,
        string? reason,
        int? referenceId = null)
    {
        Product = product;
        ProductId = product.Id;
        Type = type;
        QuantityChange = quantityChange;
        QuantityBefore = quantityBefore;
        QuantityAfter = quantityBefore + quantityChange;
        UserId = userId;
        Reason = Guard.Optional(reason, "Reason", ReasonMaxLength);
        ReferenceId = referenceId;
    }

    public int ProductId { get; private set; }
    public Product Product { get; private set; } = default!;
    public StockAdjustmentType Type { get; private set; }
    public decimal QuantityChange { get; private set; }
    public decimal QuantityBefore { get; private set; }
    public decimal QuantityAfter { get; private set; }
    public string? Reason { get; private set; }

    /// <summary>Id of the sale or return that caused the change, when applicable.</summary>
    public int? ReferenceId { get; private set; }

    public int UserId { get; private set; }
    public User User { get; private set; } = default!;
}

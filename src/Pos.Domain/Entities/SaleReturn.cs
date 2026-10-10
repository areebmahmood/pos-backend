using Pos.Domain.Common;

namespace Pos.Domain.Entities;

public class SaleReturn : AuditableEntity
{
    public const int ReasonMaxLength = 500;

    private readonly List<SaleReturnItem> _items = [];

    private SaleReturn() { }

    internal SaleReturn(Sale sale, RegisterSession register, int processedById, string? reason)
    {
        Sale = sale;
        SaleId = sale.Id;
        RegisterSessionId = register.Id;
        ProcessedById = processedById;
        Reason = Guard.Optional(reason, "Reason", ReasonMaxLength);
    }

    public int SaleId { get; private set; }
    public Sale Sale { get; private set; } = default!;
    public int RegisterSessionId { get; private set; }
    public RegisterSession RegisterSession { get; private set; } = default!;
    public int ProcessedById { get; private set; }
    public User ProcessedBy { get; private set; } = default!;
    public string? Reason { get; private set; }
    public decimal RefundAmount { get; private set; }

    /// <summary>Cost value of the goods put back into stock.</summary>
    public decimal CostAmount { get; private set; }

    public IReadOnlyCollection<SaleReturnItem> Items => _items;

    internal void AddItem(SaleItem saleItem, decimal quantity, decimal paidUnitPrice) =>
        _items.Add(new SaleReturnItem(saleItem, quantity, paidUnitPrice));

    internal void ComputeRefund()
    {
        RefundAmount = Money.Round(_items.Sum(i => i.LineTotal));
        CostAmount = Money.Round(_items.Sum(i => i.UnitCost * i.Quantity));
    }

    internal void CapRefund(decimal maximum) => RefundAmount = Money.Round(maximum);
}

using Pos.Domain.Common;
using Pos.Domain.Enums;
using Pos.Domain.Exceptions;

namespace Pos.Domain.Entities;

/// <summary>
/// A completed bill. Totals are computed by the aggregate itself so they can never disagree with the items.
/// </summary>
public class Sale : AuditableEntity
{
    public const int InvoiceNumberMaxLength = 32;
    public const int CustomerNameMaxLength = 100;
    public const int CustomerPhoneMaxLength = 30;

    private readonly List<SaleItem> _items = [];
    private readonly List<SaleReturn> _returns = [];

    private Sale() { }

    private Sale(string invoiceNumber, RegisterSession register, int cashierId, string? customerName, string? customerPhone)
    {
        InvoiceNumber = invoiceNumber;
        RegisterSessionId = register.Id;
        CashierId = cashierId;
        CustomerName = Guard.Optional(customerName, "Customer name", CustomerNameMaxLength);
        CustomerPhone = Guard.Optional(customerPhone, "Customer phone", CustomerPhoneMaxLength);
        Status = SaleStatus.Completed;
    }

    public string InvoiceNumber { get; private set; } = default!;
    public int RegisterSessionId { get; private set; }
    public RegisterSession RegisterSession { get; private set; } = default!;
    public int CashierId { get; private set; }
    public User Cashier { get; private set; } = default!;
    public string? CustomerName { get; private set; }
    public string? CustomerPhone { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }

    /// <summary>Sum of list prices.</summary>
    public decimal Subtotal { get; private set; }

    /// <summary>Discount coming from product discounted prices.</summary>
    public decimal ItemDiscount { get; private set; }

    /// <summary>Extra discount applied to the whole bill.</summary>
    public decimal BillDiscount { get; private set; }

    public decimal Total { get; private set; }
    public decimal AmountPaid { get; private set; }
    public decimal Change { get; private set; }
    public decimal ReturnedAmount { get; private set; }
    public SaleStatus Status { get; private set; }

    public IReadOnlyCollection<SaleItem> Items => _items;
    public IReadOnlyCollection<SaleReturn> Returns => _returns;

    /// <summary>Sum of item totals after product discounts, before the bill discount.</summary>
    public decimal NetBeforeBillDiscount => Subtotal - ItemDiscount;

    public static Sale Create(
        string invoiceNumber,
        RegisterSession register,
        int cashierId,
        IEnumerable<SaleLine> lines,
        PaymentMethod paymentMethod,
        decimal amountPaid,
        decimal billDiscount,
        string? customerName,
        string? customerPhone)
    {
        register.EnsureOpen();

        var sale = new Sale(invoiceNumber, register, cashierId, customerName, customerPhone);
        foreach (var line in lines)
            sale.AddItem(line.Product, line.Quantity, line.Discount);

        if (sale._items.Count == 0)
            throw new DomainException("A sale must contain at least one item.");

        sale.Checkout(paymentMethod, amountPaid, billDiscount);
        return sale;
    }

    private void AddItem(Product product, decimal quantity, decimal discount)
    {
        if (!product.IsActive || product.IsDeleted)
            throw new DomainException($"'{product.Name}' is not available for sale.");

        var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing is null)
            _items.Add(new SaleItem(product, quantity, discount));
        else
            existing.Merge(quantity, discount);
    }

    private void Checkout(PaymentMethod paymentMethod, decimal amountPaid, decimal billDiscount)
    {
        Subtotal = Money.Round(_items.Sum(i => i.UnitPrice * i.Quantity));
        ItemDiscount = Money.Round(_items.Sum(i => i.LineDiscount));

        billDiscount = Money.Round(Guard.NotNegative(billDiscount, "Bill discount"));
        if (billDiscount > NetBeforeBillDiscount)
            throw new DomainException("Bill discount cannot exceed the bill amount.");

        BillDiscount = billDiscount;
        Total = Money.Round(NetBeforeBillDiscount - BillDiscount);
        PaymentMethod = paymentMethod;

        if (paymentMethod == PaymentMethod.Cash)
        {
            amountPaid = Money.Round(amountPaid);
            if (amountPaid < Total)
                throw new DomainException($"Amount paid ({amountPaid:0.00}) is less than the total ({Total:0.00}).");

            AmountPaid = amountPaid;
            Change = AmountPaid - Total;
        }
        else
        {
            AmountPaid = Total;
            Change = 0;
        }
    }

    /// <summary>
    /// Refund value per unit for an item: what the customer actually paid, including the item's share of the bill discount.
    /// </summary>
    public decimal PaidUnitPrice(SaleItem item)
    {
        if (BillDiscount == 0 || NetBeforeBillDiscount == 0)
            return item.NetUnitPrice;

        var factor = 1 - BillDiscount / NetBeforeBillDiscount;
        return item.NetUnitPrice * factor;
    }

    public SaleReturn ProcessReturn(
        RegisterSession register,
        int processedById,
        IEnumerable<(int SaleItemId, decimal Quantity)> lines,
        string? reason)
    {
        register.EnsureOpen();
        if (Status == SaleStatus.Returned)
            throw new DomainException("This sale has already been fully returned.");

        var saleReturn = new SaleReturn(this, register, processedById, reason);
        foreach (var (saleItemId, quantity) in lines)
        {
            var item = _items.FirstOrDefault(i => i.Id == saleItemId)
                ?? throw new DomainException($"Item {saleItemId} does not belong to sale {InvoiceNumber}.");

            item.RegisterReturn(quantity);
            saleReturn.AddItem(item, quantity, PaidUnitPrice(item));
        }

        if (saleReturn.Items.Count == 0)
            throw new DomainException("Select at least one item to return.");

        saleReturn.ComputeRefund();

        // Never refund more than was paid, regardless of rounding across partial returns.
        var remaining = Total - ReturnedAmount;
        if (saleReturn.RefundAmount > remaining)
            saleReturn.CapRefund(remaining);

        ReturnedAmount = Money.Round(ReturnedAmount + saleReturn.RefundAmount);
        Status = _items.All(i => i.ReturnableQuantity == 0) ? SaleStatus.Returned : SaleStatus.PartiallyReturned;
        _returns.Add(saleReturn);
        return saleReturn;
    }
}

/// <summary>A requested bill line: the product, quantity and any extra discount amount for the whole line.</summary>
public readonly record struct SaleLine(Product Product, decimal Quantity, decimal Discount = 0);

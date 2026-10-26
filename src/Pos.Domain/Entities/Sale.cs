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
    public int? CustomerId { get; private set; }
    public Customer? Customer { get; private set; }

    /// <summary>The discount code used on this bill, as typed at the time of sale.</summary>
    public string? DiscountCode { get; private set; }
    public int? DiscountCodeId { get; private set; }

    /// <summary>The part of <see cref="BillDiscount"/> that came from <see cref="DiscountCode"/>.</summary>
    public decimal CodeDiscount { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    /// <summary>The online channel, set only when <see cref="PaymentMethod"/> is <see cref="PaymentMethod.Online"/>.</summary>
    public OnlinePaymentType? OnlinePaymentType { get; private set; }

    /// <summary>Sum of list prices.</summary>
    public decimal Subtotal { get; private set; }

    /// <summary>Discount coming from product discounted prices.</summary>
    public decimal ItemDiscount { get; private set; }

    /// <summary>Discount on the whole bill: the cashier's bill discount plus any <see cref="CodeDiscount"/>.</summary>
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
        string? customerPhone,
        Customer? customer = null,
        DiscountCode? discountCode = null,
        DateOnly? today = null,
        OnlinePaymentType? onlinePaymentType = null)
    {
        register.EnsureOpen();

        var sale = new Sale(invoiceNumber, register, cashierId, customerName, customerPhone);
        foreach (var line in lines)
            sale.AddItem(line.Product, line.Quantity, line.UnitDiscount);

        if (sale._items.Count == 0)
            throw new DomainException("A sale must contain at least one item.");

        if (customer is not null)
            sale.LinkCustomer(customer);

        sale.Checkout(paymentMethod, onlinePaymentType, amountPaid, billDiscount, discountCode, today);
        return sale;
    }

    private void AddItem(Product product, decimal quantity, decimal unitDiscount)
    {
        if (!product.IsActive || product.IsDeleted)
            throw new DomainException($"'{product.Name}' is not available for sale.");

        var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing is null)
            _items.Add(new SaleItem(product, quantity, unitDiscount));
        else
            existing.Merge(quantity, unitDiscount);
    }

    /// <summary>Links the bill to a customer record (also used to backfill older sales).</summary>
    public void LinkCustomer(Customer customer)
    {
        Customer = customer;
        if (customer.Id != 0)
            CustomerId = customer.Id; // A new customer's key is filled in when it is saved.
    }

    private void Checkout(
        PaymentMethod paymentMethod,
        OnlinePaymentType? onlinePaymentType,
        decimal amountPaid,
        decimal billDiscount,
        DiscountCode? discountCode,
        DateOnly? today)
    {
        if (paymentMethod == PaymentMethod.Online && onlinePaymentType is null)
            throw new DomainException("Select the online payment type (Jazz Cash, Easy Paisa or Bank Transfer).");

        Subtotal = Money.Round(_items.Sum(i => i.UnitPrice * i.Quantity));
        ItemDiscount = Money.Round(_items.Sum(i => i.LineDiscount));

        billDiscount = Money.Round(Guard.NotNegative(billDiscount, "Bill discount"));
        if (billDiscount > NetBeforeBillDiscount)
            throw new DomainException("Bill discount cannot exceed the bill amount.");

        if (discountCode is not null)
        {
            // The code applies to the amount after item discounts and the cashier's bill discount.
            var amount = NetBeforeBillDiscount - billDiscount;
            discountCode.EnsureUsable(today ?? DateOnly.FromDateTime(DateTime.UtcNow), amount);
            CodeDiscount = discountCode.DiscountFor(amount);
            DiscountCode = discountCode.Code;
            DiscountCodeId = discountCode.Id;
            discountCode.RegisterUse();
        }

        BillDiscount = billDiscount + CodeDiscount;
        Total = Money.Round(NetBeforeBillDiscount - BillDiscount);
        PaymentMethod = paymentMethod;
        OnlinePaymentType = paymentMethod == PaymentMethod.Online ? onlinePaymentType : null;

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

    /// <summary>
    /// Prepares the sale for deletion: returns the quantity still with the customer for each item
    /// (returned units were already put back in stock) and frees up the discount code use.
    /// </summary>
    public IReadOnlyList<(SaleItem Item, decimal Quantity)> ReleaseForDeletion(DiscountCode? discountCode)
    {
        if (discountCode is not null && discountCode.Id == DiscountCodeId)
            discountCode.ReleaseUse();

        return _items.Where(i => i.ReturnableQuantity > 0).Select(i => (i, i.ReturnableQuantity)).ToList();
    }
}

/// <summary>A requested bill line: the product, quantity and any extra discount per unit.</summary>
public readonly record struct SaleLine(Product Product, decimal Quantity, decimal UnitDiscount = 0);

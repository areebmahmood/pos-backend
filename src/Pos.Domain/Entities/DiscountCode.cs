using Pos.Domain.Common;
using Pos.Domain.Enums;
using Pos.Domain.Exceptions;

namespace Pos.Domain.Entities;

/// <summary>A code the cashier enters at billing for a discount on the whole bill.</summary>
public class DiscountCode : AuditableEntity
{
    public const int CodeMaxLength = 30;
    public const int DescriptionMaxLength = 200;

    private DiscountCode() { }

    public DiscountCode(
        string code, string? description, DiscountType type, decimal value, decimal? minimumBill,
        decimal? maximumDiscount, DateOnly? validFrom, DateOnly? validTo, int? usageLimit, bool isActive)
    {
        Update(code, description, type, value, minimumBill, maximumDiscount, validFrom, validTo, usageLimit, isActive);
    }

    public string Code { get; private set; } = default!;
    public string? Description { get; private set; }
    public DiscountType Type { get; private set; }

    /// <summary>Percent (0–100] for <see cref="DiscountType.Percentage"/>, otherwise an amount.</summary>
    public decimal Value { get; private set; }

    public decimal? MinimumBill { get; private set; }

    /// <summary>Upper limit on the discount a percentage code gives.</summary>
    public decimal? MaximumDiscount { get; private set; }

    /// <summary>Store-local dates, inclusive.</summary>
    public DateOnly? ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }

    public int? UsageLimit { get; private set; }
    public int UsedCount { get; private set; }
    public bool IsActive { get; private set; }

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    public void Update(
        string code, string? description, DiscountType type, decimal value, decimal? minimumBill,
        decimal? maximumDiscount, DateOnly? validFrom, DateOnly? validTo, int? usageLimit, bool isActive)
    {
        code = NormalizeCode(Guard.NotEmpty(code, "Code", CodeMaxLength));
        if (!code.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
            throw new DomainException("Code can only contain letters, digits, '-' and '_'.");

        Guard.Positive(value, "Discount value");
        if (type == DiscountType.Percentage && value > 100)
            throw new DomainException("A percentage discount cannot be more than 100.");
        if (validFrom is not null && validTo is not null && validTo < validFrom)
            throw new DomainException("The end date must be on or after the start date.");
        if (usageLimit is <= 0)
            throw new DomainException("Usage limit must be greater than zero.");

        Code = code;
        Description = Guard.Optional(description, "Description", DescriptionMaxLength);
        Type = type;
        Value = Money.Round(value);
        MinimumBill = minimumBill is null ? null : Money.Round(Guard.NotNegative(minimumBill.Value, "Minimum bill"));
        MaximumDiscount = type == DiscountType.Percentage && maximumDiscount is not null
            ? Money.Round(Guard.Positive(maximumDiscount.Value, "Maximum discount"))
            : null;
        ValidFrom = validFrom;
        ValidTo = validTo;
        UsageLimit = usageLimit;
        IsActive = isActive;
    }

    /// <summary>Throws when the code cannot be used today on a bill of <paramref name="billAmount"/>.</summary>
    public void EnsureUsable(DateOnly today, decimal billAmount)
    {
        if (!IsActive)
            throw new DomainException($"Discount code {Code} is not active.");
        if (ValidFrom is not null && today < ValidFrom)
            throw new DomainException($"Discount code {Code} is valid from {ValidFrom:dd MMM yyyy}.");
        if (ValidTo is not null && today > ValidTo)
            throw new DomainException($"Discount code {Code} expired on {ValidTo:dd MMM yyyy}.");
        if (UsageLimit is not null && UsedCount >= UsageLimit)
            throw new DomainException($"Discount code {Code} has reached its usage limit.");
        if (MinimumBill is not null && billAmount < MinimumBill)
            throw new DomainException($"Discount code {Code} needs a bill of at least {MinimumBill:0.##}.");
    }

    /// <summary>The discount this code gives on <paramref name="amount"/>, never more than the amount itself.</summary>
    public decimal DiscountFor(decimal amount)
    {
        if (amount <= 0)
            return 0;

        var discount = Type == DiscountType.Percentage ? amount * Value / 100 : Value;
        if (MaximumDiscount is not null)
            discount = Math.Min(discount, MaximumDiscount.Value);

        return Money.Round(Math.Min(discount, amount));
    }

    internal void RegisterUse() => UsedCount++;

    /// <summary>Gives back a use when the sale that used the code is deleted.</summary>
    internal void ReleaseUse() => UsedCount = Math.Max(0, UsedCount - 1);
}

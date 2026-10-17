using Pos.Application.Common.Models;
using Pos.Domain.Enums;

namespace Pos.Application.Features.DiscountCodes;

public sealed record DiscountCodeDto(
    int Id,
    string Code,
    string? Description,
    DiscountType Type,
    decimal Value,
    decimal? MinimumBill,
    decimal? MaximumDiscount,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    int? UsageLimit,
    int UsedCount,
    bool IsActive,
    DateTime CreatedAt);

public sealed record SaveDiscountCodeRequest(
    string Code,
    string? Description,
    DiscountType Type,
    decimal Value,
    decimal? MinimumBill,
    decimal? MaximumDiscount,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    int? UsageLimit,
    bool IsActive = true);

public sealed record DiscountCodeQuery : PageRequest
{
    public string? Search { get; init; }
}

/// <summary>What a code would take off a bill whose amount after item discounts and the bill discount is <c>amount</c>.</summary>
public sealed record DiscountCodeCheckDto(
    string Code,
    string? Description,
    DiscountType Type,
    decimal Value,
    decimal? MinimumBill,
    decimal? MaximumDiscount,
    decimal Discount);

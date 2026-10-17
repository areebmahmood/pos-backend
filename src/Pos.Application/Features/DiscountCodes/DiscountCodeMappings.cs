using System.Linq.Expressions;
using Pos.Domain.Entities;

namespace Pos.Application.Features.DiscountCodes;

public static class DiscountCodeMappings
{
    public static readonly Expression<Func<DiscountCode, DiscountCodeDto>> Projection = d => new DiscountCodeDto(
        d.Id, d.Code, d.Description, d.Type, d.Value, d.MinimumBill, d.MaximumDiscount,
        d.ValidFrom, d.ValidTo, d.UsageLimit, d.UsedCount, d.IsActive, d.CreatedAt);
}

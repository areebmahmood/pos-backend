using FluentValidation;

namespace Pos.Application.Common.Validation;

public static class RuleExtensions
{
    public static IRuleBuilderOptions<T, decimal> Money<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);

    public static IRuleBuilderOptions<T, decimal?> Money<T>(this IRuleBuilder<T, decimal?> rule) =>
        rule.GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);

    public static IRuleBuilderOptions<T, decimal> Quantity<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.PrecisionScale(18, 3, ignoreTrailingZeros: true);
}

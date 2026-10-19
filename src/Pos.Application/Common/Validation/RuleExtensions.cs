using FluentValidation;
using Pos.Domain.Common;

namespace Pos.Application.Common.Validation;

public static class RuleExtensions
{
    public static IRuleBuilderOptions<T, decimal> Money<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);

    public static IRuleBuilderOptions<T, decimal?> Money<T>(this IRuleBuilder<T, decimal?> rule) =>
        rule.GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);

    public static IRuleBuilderOptions<T, decimal> Quantity<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.PrecisionScale(18, 3, ignoreTrailingZeros: true);

    /// <summary>A Pakistani mobile number (see <see cref="PhoneNumber.IsValidMobile"/>); empty values pass, so pair with NotEmpty when required.</summary>
    public static IRuleBuilderOptions<T, string?> MobileNumber<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(phone => string.IsNullOrWhiteSpace(phone) || PhoneNumber.IsValidMobile(phone))
            .WithMessage($"Enter a valid mobile number: {PhoneNumber.MobileFormatHint}.");
}

using FluentValidation;
using Pos.Application.Common.Validation;
using Pos.Domain.Entities;
using Pos.Domain.Enums;

namespace Pos.Application.Features.DiscountCodes;

public sealed class SaveDiscountCodeRequestValidator : AbstractValidator<SaveDiscountCodeRequest>
{
    public SaveDiscountCodeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(DiscountCode.CodeMaxLength)
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("Use only letters, digits, '-' and '_'.");
        RuleFor(x => x.Description).MaximumLength(DiscountCode.DescriptionMaxLength);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Value).GreaterThan(0).Money();
        RuleFor(x => x.Value).LessThanOrEqualTo(100).When(x => x.Type == DiscountType.Percentage)
            .WithMessage("A percentage cannot be more than 100.");
        RuleFor(x => x.MinimumBill).Money();
        RuleFor(x => x.MaximumDiscount).GreaterThan(0).Money();
        RuleFor(x => x.UsageLimit).GreaterThan(0);
        RuleFor(x => x.ValidTo).GreaterThanOrEqualTo(x => x.ValidFrom)
            .When(x => x.ValidFrom is not null && x.ValidTo is not null)
            .WithMessage("The end date must be on or after the start date.");
    }
}

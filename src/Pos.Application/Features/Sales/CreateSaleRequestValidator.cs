using FluentValidation;
using Pos.Application.Common.Validation;
using Pos.Domain.Entities;
using Pos.Domain.Enums;

namespace Pos.Application.Features.Sales;

public sealed class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
{
    public const int MaxLines = 500;

    public CreateSaleRequestValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("Add at least one item to the bill.")
            .Must(items => items.Count <= MaxLines).WithMessage($"A bill can have at most {MaxLines} lines.");
        RuleForEach(x => x.Items).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0).Quantity();
            line.RuleFor(l => l.Discount).Money();
        });
        RuleFor(x => x.PaymentMethod).IsInEnum();
        RuleFor(x => x.OnlinePaymentType).IsInEnum();
        RuleFor(x => x.OnlinePaymentType).NotNull()
            .When(x => x.PaymentMethod == PaymentMethod.Online)
            .WithMessage("Select the online payment type (Jazz Cash, Easy Paisa or Bank Transfer).");
        RuleFor(x => x.AmountPaid).Money();
        RuleFor(x => x.BillDiscount).Money();
        RuleFor(x => x.CustomerName).MaximumLength(Sale.CustomerNameMaxLength);
        RuleFor(x => x.CustomerPhone).MaximumLength(Sale.CustomerPhoneMaxLength).MobileNumber();
        RuleFor(x => x.DiscountCode).MaximumLength(DiscountCode.CodeMaxLength);
    }
}

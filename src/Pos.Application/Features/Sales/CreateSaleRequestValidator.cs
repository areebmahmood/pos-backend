using FluentValidation;
using Pos.Application.Common.Validation;
using Pos.Domain.Entities;

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
        });
        RuleFor(x => x.PaymentMethod).IsInEnum();
        RuleFor(x => x.AmountPaid).Money();
        RuleFor(x => x.BillDiscount).Money();
        RuleFor(x => x.CustomerName).MaximumLength(Sale.CustomerNameMaxLength);
        RuleFor(x => x.CustomerPhone).MaximumLength(Sale.CustomerPhoneMaxLength);
    }
}

using FluentValidation;
using Pos.Application.Common.Validation;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Returns;

public sealed class CreateReturnRequestValidator : AbstractValidator<CreateReturnRequest>
{
    public CreateReturnRequestValidator()
    {
        RuleFor(x => x.SaleId).GreaterThan(0);
        RuleFor(x => x.Reason).MaximumLength(SaleReturn.ReasonMaxLength);
        RuleFor(x => x.Items).NotEmpty().WithMessage("Select at least one item to return.");
        RuleForEach(x => x.Items).ChildRules(line =>
        {
            line.RuleFor(l => l.SaleItemId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0).Quantity();
        });
        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.SaleItemId).Distinct().Count() == items.Count)
            .WithMessage("Each sale item may appear only once.");
    }
}

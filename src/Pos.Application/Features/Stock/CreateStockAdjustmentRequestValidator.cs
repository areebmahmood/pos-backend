using FluentValidation;
using Pos.Application.Common.Validation;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Stock;

public sealed class CreateStockAdjustmentRequestValidator : AbstractValidator<CreateStockAdjustmentRequest>
{
    public CreateStockAdjustmentRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.QuantityChange).NotEqual(0).WithMessage("Quantity change cannot be zero.").Quantity();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(StockAdjustment.ReasonMaxLength);
    }
}

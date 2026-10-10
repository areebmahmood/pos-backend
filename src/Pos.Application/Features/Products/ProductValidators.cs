using FluentValidation;
using Pos.Application.Common.Validation;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Products;

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Product.NameMaxLength);
        RuleFor(x => x.Barcode).MaximumLength(Product.BarcodeMaxLength)
            .Matches("^[0-9A-Za-z-]*$").WithMessage("Barcode may only contain letters, digits and '-'.");
        RuleFor(x => x.Description).MaximumLength(Product.DescriptionMaxLength);
        RuleFor(x => x.CostPrice).Money();
        RuleFor(x => x.SalePrice).Money().GreaterThan(0);
        RuleFor(x => x.DiscountedPrice).Money().LessThan(x => x.SalePrice)
            .WithMessage("Discounted price must be lower than the sale price.");
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0).Quantity();
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0).Quantity();
    }
}

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Product.NameMaxLength);
        RuleFor(x => x.Barcode).MaximumLength(Product.BarcodeMaxLength)
            .Matches("^[0-9A-Za-z-]*$").WithMessage("Barcode may only contain letters, digits and '-'.");
        RuleFor(x => x.Description).MaximumLength(Product.DescriptionMaxLength);
        RuleFor(x => x.CostPrice).Money();
        RuleFor(x => x.SalePrice).Money().GreaterThan(0);
        RuleFor(x => x.DiscountedPrice).Money().LessThan(x => x.SalePrice)
            .WithMessage("Discounted price must be lower than the sale price.");
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0).Quantity();
    }
}

public sealed class SetDiscountRequestValidator : AbstractValidator<SetDiscountRequest>
{
    public SetDiscountRequestValidator()
    {
        RuleFor(x => x.DiscountedPrice).Money();
    }
}

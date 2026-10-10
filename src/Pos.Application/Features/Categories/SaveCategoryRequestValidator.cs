using FluentValidation;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Categories;

public sealed class SaveCategoryRequestValidator : AbstractValidator<SaveCategoryRequest>
{
    public SaveCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Category.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(Category.DescriptionMaxLength);
    }
}

using FluentValidation;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Customers;

public sealed class SaveCustomerRequestValidator : AbstractValidator<SaveCustomerRequest>
{
    public SaveCustomerRequestValidator()
    {
        RuleFor(x => x.Name).MaximumLength(Customer.NameMaxLength);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(Sale.CustomerPhoneMaxLength);
    }
}

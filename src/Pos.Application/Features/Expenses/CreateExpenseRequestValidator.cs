using FluentValidation;
using Pos.Application.Common.Validation;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Expenses;

public sealed class CreateExpenseRequestValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(Expense.TitleMaxLength);
        RuleFor(x => x.Category).MaximumLength(Expense.CategoryMaxLength);
        RuleFor(x => x.Amount).Money().GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(Expense.NotesMaxLength);
    }
}

using System.Linq.Expressions;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Expenses;

public static class ExpenseMappings
{
    public static readonly Expression<Func<Expense, ExpenseDto>> Projection = e => new ExpenseDto(
        e.Id, e.Title, e.Category, e.Amount, e.Notes, e.RegisterSessionId, e.CreatedBy.FullName, e.CreatedAt);
}

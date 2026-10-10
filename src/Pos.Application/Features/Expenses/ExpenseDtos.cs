using Pos.Application.Common.Models;

namespace Pos.Application.Features.Expenses;

public sealed record ExpenseDto(
    int Id,
    string Title,
    string? Category,
    decimal Amount,
    string? Notes,
    int RegisterSessionId,
    string CreatedBy,
    DateTime CreatedAt);

public sealed record CreateExpenseRequest(string Title, string? Category, decimal Amount, string? Notes);

public sealed record ExpenseQuery : PageRequest
{
    public string? Search { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
}

public sealed record ExpenseFilter(PageRequest Page, string? Search, DateRange? Range, int? CreatedById);

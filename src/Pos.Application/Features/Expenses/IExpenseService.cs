using Pos.Application.Common.Models;

namespace Pos.Application.Features.Expenses;

public interface IExpenseService
{
    Task<PagedResult<ExpenseDto>> ListAsync(ExpenseQuery query, CancellationToken ct);

    Task<ExpenseDto> GetAsync(int id, CancellationToken ct);

    Task<ExpenseDto> CreateAsync(CreateExpenseRequest request, CancellationToken ct);

    Task DeleteAsync(int id, CancellationToken ct);
}

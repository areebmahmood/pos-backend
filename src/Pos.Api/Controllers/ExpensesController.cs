using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Common.Models;
using Pos.Application.Features.Expenses;
using Pos.Domain.Common;

namespace Pos.Api.Controllers;

[Route("api/expenses")]
public sealed class ExpensesController(IExpenseService expenses) : ApiControllerBase
{
    [HttpGet]
    public Task<PagedResult<ExpenseDto>> List([FromQuery] ExpenseQuery query, CancellationToken ct) =>
        expenses.ListAsync(query, ct);

    [HttpGet("{id:int}")]
    public Task<ExpenseDto> Get(int id, CancellationToken ct) => expenses.GetAsync(id, ct);

    [HttpPost]
    [ProducesResponseType<ExpenseDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateExpenseRequest request, CancellationToken ct)
    {
        var expense = await expenses.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = expense.Id }, expense);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await expenses.DeleteAsync(id, ct);
        return NoContent();
    }
}

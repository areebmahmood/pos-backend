using FluentValidation;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Abstractions.Services;
using Pos.Application.Common.Exceptions;
using Pos.Application.Common.Models;
using Pos.Application.Common.Validation;
using Pos.Application.Features.Registers;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Expenses;

public sealed class ExpenseService(
    IExpenseRepository expenses,
    IRegisterSessionRepository registers,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IStoreClock storeClock,
    IValidator<CreateExpenseRequest> validator) : IExpenseService
{
    public Task<PagedResult<ExpenseDto>> ListAsync(ExpenseQuery query, CancellationToken ct)
    {
        var createdById = currentUser.IsAdmin ? (int?)null : currentUser.UserId;
        var filter = new ExpenseFilter(query, query.Search, storeClock.ToUtcRange(query.From, query.To), createdById);
        return expenses.ListAsync(filter, ExpenseMappings.Projection, ct);
    }

    public async Task<ExpenseDto> GetAsync(int id, CancellationToken ct) =>
        await expenses.FindAsync(id, ExpenseMappings.Projection, ct) ?? throw NotFoundException.For("Expense", id);

    public async Task<ExpenseDto> CreateAsync(CreateExpenseRequest request, CancellationToken ct)
    {
        await validator.EnsureValidAsync(request, ct);
        var register = await registers.RequireOpenForUserAsync(currentUser.UserId, "recording expenses", ct);

        var expense = new Expense(request.Title, request.Category, request.Amount, request.Notes, register, currentUser.UserId);
        expenses.Add(expense);
        await unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(expense.Id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var expense = await expenses.GetByIdAsync(id, ct) ?? throw NotFoundException.For("Expense", id);
        expenses.Remove(expense);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

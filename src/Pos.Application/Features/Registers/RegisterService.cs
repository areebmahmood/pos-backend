using FluentValidation;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Abstractions.Services;
using Pos.Application.Common.Exceptions;
using Pos.Application.Common.Models;
using Pos.Application.Common.Validation;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Registers;

public sealed class RegisterService(
    IRegisterSessionRepository registers,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    IValidator<OpenRegisterRequest> openValidator,
    IValidator<CloseRegisterRequest> closeValidator) : IRegisterService
{
    public async Task<RegisterSessionDto?> GetCurrentAsync(CancellationToken ct)
    {
        var register = await registers.GetOpenForUserAsync(currentUser.UserId, ct);
        return register is null ? null : await GetAsync(register.Id, ct);
    }

    public async Task<RegisterSessionDto> GetAsync(int id, CancellationToken ct)
    {
        var row = await registers.FindAsync(id, RegisterMappings.Projection, ct) ?? throw NotFoundException.For("Register", id);
        EnsureCanAccess(row.UserId);

        var totals = await registers.GetTotalsAsync([id], ct);
        return row.ToDto(totals.GetValueOrDefault(id, RegisterTotals.Empty));
    }

    public async Task<PagedResult<RegisterSessionDto>> ListAsync(RegisterQuery query, CancellationToken ct)
    {
        if (!currentUser.IsAdmin)
            query = query with { UserId = currentUser.UserId };

        var page = await registers.ListAsync(query, RegisterMappings.Projection, ct);
        return await AttachTotalsAsync(page, ct);
    }

    public async Task<IReadOnlyList<RegisterSessionDto>> ListOpenAsync(CancellationToken ct)
    {
        var rows = await registers.ListOpenAsync(RegisterMappings.Projection, ct);
        var totals = await registers.GetTotalsAsync(rows.Select(r => r.Id).ToList(), ct);
        return rows.Select(r => r.ToDto(totals.GetValueOrDefault(r.Id, RegisterTotals.Empty))).ToList();
    }

    public async Task<RegisterSessionDto> OpenAsync(OpenRegisterRequest request, CancellationToken ct)
    {
        await openValidator.EnsureValidAsync(request, ct);
        if (await registers.HasOpenForUserAsync(currentUser.UserId, ct))
            throw new ConflictException("You already have an open register. Close it before opening a new one.");

        var register = new RegisterSession(currentUser.UserId, request.OpeningCash, request.Notes, clock.UtcNow);
        registers.Add(register);
        await unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(register.Id, ct);
    }

    public async Task<RegisterSessionDto> CloseAsync(int id, CloseRegisterRequest request, CancellationToken ct)
    {
        await closeValidator.EnsureValidAsync(request, ct);
        var register = await registers.GetByIdAsync(id, ct) ?? throw NotFoundException.For("Register", id);
        EnsureCanAccess(register.UserId);

        var totals = (await registers.GetTotalsAsync([id], ct)).GetValueOrDefault(id, RegisterTotals.Empty);
        var expected = RegisterMappings.ExpectedCash(register.OpeningCash, totals);

        register.Close(request.ClosingCash, expected, request.Notes, currentUser.UserId, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    private async Task<PagedResult<RegisterSessionDto>> AttachTotalsAsync(PagedResult<RegisterRow> page, CancellationToken ct)
    {
        var totals = await registers.GetTotalsAsync(page.Items.Select(r => r.Id).ToList(), ct);
        return page.Map(r => r.ToDto(totals.GetValueOrDefault(r.Id, RegisterTotals.Empty)));
    }

    private void EnsureCanAccess(int ownerId)
    {
        if (!currentUser.IsAdmin && ownerId != currentUser.UserId)
            throw new ForbiddenException("You can only access your own register.");
    }
}

using FluentValidation;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Abstractions.Services;
using Pos.Application.Common.Exceptions;
using Pos.Application.Common.Models;
using Pos.Application.Common.Validation;
using Pos.Domain.Entities;

namespace Pos.Application.Features.DiscountCodes;

public sealed class DiscountCodeService(
    IDiscountCodeRepository codes,
    IUnitOfWork unitOfWork,
    IStoreClock storeClock,
    IValidator<SaveDiscountCodeRequest> validator) : IDiscountCodeService
{
    public Task<PagedResult<DiscountCodeDto>> ListAsync(DiscountCodeQuery query, CancellationToken ct) =>
        codes.ListAsync(query, query.Search, DiscountCodeMappings.Projection, ct);

    public async Task<DiscountCodeDto> GetAsync(int id, CancellationToken ct) =>
        await codes.FindAsync(id, DiscountCodeMappings.Projection, ct) ?? throw NotFoundException.For("Discount code", id);

    public async Task<DiscountCodeDto> CreateAsync(SaveDiscountCodeRequest request, CancellationToken ct)
    {
        await validator.EnsureValidAsync(request, ct);
        await EnsureUniqueAsync(request.Code, null, ct);

        var code = new DiscountCode(
            request.Code, request.Description, request.Type, request.Value, request.MinimumBill,
            request.MaximumDiscount, request.ValidFrom, request.ValidTo, request.UsageLimit, request.IsActive);
        codes.Add(code);
        await unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(code.Id, ct);
    }

    public async Task<DiscountCodeDto> UpdateAsync(int id, SaveDiscountCodeRequest request, CancellationToken ct)
    {
        await validator.EnsureValidAsync(request, ct);
        var code = await codes.GetByIdAsync(id, ct) ?? throw NotFoundException.For("Discount code", id);
        await EnsureUniqueAsync(request.Code, id, ct);

        code.Update(
            request.Code, request.Description, request.Type, request.Value, request.MinimumBill,
            request.MaximumDiscount, request.ValidFrom, request.ValidTo, request.UsageLimit, request.IsActive);
        await unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var code = await codes.GetByIdAsync(id, ct) ?? throw NotFoundException.For("Discount code", id);
        codes.Remove(code);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<DiscountCodeCheckDto> CheckAsync(string code, decimal amount, CancellationToken ct)
    {
        var normalized = DiscountCode.NormalizeCode(code ?? "");
        var entity = await codes.GetByCodeAsync(normalized, ct) ?? throw new NotFoundException($"Discount code '{normalized}' does not exist.");
        entity.EnsureUsable(storeClock.Today, amount);
        return new DiscountCodeCheckDto(
            entity.Code, entity.Description, entity.Type, entity.Value, entity.MinimumBill, entity.MaximumDiscount,
            entity.DiscountFor(amount));
    }

    private async Task EnsureUniqueAsync(string code, int? excludeId, CancellationToken ct)
    {
        var normalized = DiscountCode.NormalizeCode(code);
        if (await codes.CodeExistsAsync(normalized, excludeId, ct))
            throw new ConflictException($"The code {normalized} already exists.");
    }
}

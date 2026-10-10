using FluentValidation;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Abstractions.Services;
using Pos.Application.Common.Exceptions;
using Pos.Application.Common.Models;
using Pos.Application.Common.Validation;
using Pos.Domain.Entities;
using Pos.Domain.Enums;

namespace Pos.Application.Features.Stock;

public sealed class StockService(
    IProductRepository products,
    IStockAdjustmentRepository adjustments,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IValidator<CreateStockAdjustmentRequest> validator) : IStockService
{
    public Task<PagedResult<StockAdjustmentDto>> ListAsync(StockAdjustmentQuery query, CancellationToken ct) =>
        adjustments.ListAsync(query, StockMappings.Projection, ct);

    public async Task<StockAdjustmentDto> AdjustAsync(CreateStockAdjustmentRequest request, CancellationToken ct)
    {
        await validator.EnsureValidAsync(request, ct);
        var product = await products.GetByIdAsync(request.ProductId, ct)
            ?? throw NotFoundException.For("Product", request.ProductId);

        var before = product.AdjustStock(request.QuantityChange);
        var adjustment = new StockAdjustment(
            product, StockAdjustmentType.Manual, request.QuantityChange, before, currentUser.UserId, request.Reason);
        adjustments.Add(adjustment);
        await unitOfWork.SaveChangesAsync(ct);

        return (await adjustments.FindAsync(adjustment.Id, StockMappings.Projection, ct))!;
    }
}

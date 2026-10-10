using FluentValidation;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Abstractions.Services;
using Pos.Application.Common.Exceptions;
using Pos.Application.Common.Models;
using Pos.Application.Common.Validation;
using Pos.Application.Features.Registers;
using Pos.Domain.Entities;
using Pos.Domain.Enums;

namespace Pos.Application.Features.Returns;

public sealed class ReturnService(
    ISaleRepository sales,
    ISaleReturnRepository returns,
    IProductRepository products,
    IRegisterSessionRepository registers,
    IStockAdjustmentRepository stockAdjustments,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IStoreClock storeClock,
    IValidator<CreateReturnRequest> validator) : IReturnService
{
    public async Task<SaleReturnDto> CreateAsync(CreateReturnRequest request, CancellationToken ct)
    {
        await validator.EnsureValidAsync(request, ct);

        var returnId = await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var register = await registers.RequireOpenForUserAsync(currentUser.UserId, "processing returns", token);
            var sale = await sales.GetWithItemsAsync(request.SaleId, token) ?? throw NotFoundException.For("Sale", request.SaleId);
            var saleReturn = sale.ProcessReturn(
                register, currentUser.UserId, request.Items.Select(i => (i.SaleItemId, i.Quantity)), request.Reason);

            var productIds = saleReturn.Items.Select(i => i.ProductId).Distinct().ToList();
            var productsById = (await products.GetByIdsAsync(productIds, token)).ToDictionary(p => p.Id);

            // Products deleted after the sale are not restocked.
            var restocked = new List<(Product Product, decimal Quantity, decimal Before)>();
            foreach (var item in saleReturn.Items)
            {
                if (productsById.TryGetValue(item.ProductId, out var product))
                    restocked.Add((product, item.Quantity, product.AdjustStock(item.Quantity)));
            }

            await unitOfWork.SaveChangesAsync(token);

            foreach (var (product, quantity, before) in restocked)
            {
                stockAdjustments.Add(new StockAdjustment(
                    product, StockAdjustmentType.Return, quantity, before, currentUser.UserId,
                    $"Return against {sale.InvoiceNumber}", saleReturn.Id));
            }

            await unitOfWork.SaveChangesAsync(token);
            return saleReturn.Id;
        }, ct);

        return await GetAsync(returnId, ct);
    }

    public async Task<SaleReturnDto> GetAsync(int id, CancellationToken ct) =>
        await returns.FindAsync(id, ReturnMappings.Projection, ct) ?? throw NotFoundException.For("Return", id);

    public Task<PagedResult<SaleReturnDto>> ListAsync(ReturnQuery query, CancellationToken ct)
    {
        var processedById = currentUser.IsAdmin ? (int?)null : currentUser.UserId;
        var filter = new ReturnFilter(query, query.Search, storeClock.ToUtcRange(query.From, query.To), processedById);
        return returns.ListAsync(filter, ReturnMappings.Projection, ct);
    }
}

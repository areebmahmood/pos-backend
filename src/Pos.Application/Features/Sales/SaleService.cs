using FluentValidation;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Abstractions.Services;
using Pos.Application.Common.Exceptions;
using Pos.Application.Common.Models;
using Pos.Application.Common.Validation;
using Pos.Application.Features.Messaging;
using Pos.Application.Features.Registers;
using Pos.Application.Features.Settings;
using Pos.Domain.Common;
using Pos.Domain.Entities;
using Pos.Domain.Enums;

namespace Pos.Application.Features.Sales;

public sealed class SaleService(
    ISaleRepository sales,
    IProductRepository products,
    IRegisterSessionRepository registers,
    IStockAdjustmentRepository stockAdjustments,
    IStoreSettingsRepository settings,
    ICustomerRepository customers,
    IDiscountCodeRepository discountCodes,
    IMessageRepository messages,
    IMessageDispatchSignal dispatchSignal,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork,
    IInvoiceNumberGenerator invoiceNumbers,
    ICurrentUser currentUser,
    IStoreClock storeClock,
    IValidator<CreateSaleRequest> validator) : ISaleService
{
    public async Task<SaleDto> CreateAsync(CreateSaleRequest request, CancellationToken ct)
    {
        await validator.EnsureValidAsync(request, ct);
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();

        var saleId = await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var register = await registers.RequireOpenForUserAsync(currentUser.UserId, "billing", token);
            var productsById = (await products.GetByIdsAsync(productIds, token)).ToDictionary(p => p.Id);
            var missing = productIds.Where(id => !productsById.ContainsKey(id)).ToList();
            if (missing.Count > 0)
                throw new NotFoundException($"Products not found: {string.Join(", ", missing)}.");

            var store = await settings.GetAsync(token);
            var customer = await FindOrCreateCustomerAsync(request, store, token);
            var discountCode = await FindDiscountCodeAsync(request.DiscountCode, token);

            var sale = Sale.Create(
                invoiceNumbers.Next(),
                register,
                currentUser.UserId,
                request.Items.Select(i => new SaleLine(productsById[i.ProductId], i.Quantity, i.Discount)),
                request.PaymentMethod,
                request.AmountPaid,
                request.BillDiscount,
                request.CustomerName,
                request.CustomerPhone,
                customer,
                discountCode,
                storeClock.Today,
                request.OnlinePaymentType);

            var stockBefore = sale.Items.ToDictionary(
                item => item.ProductId,
                item => productsById[item.ProductId].AdjustStock(-item.Quantity));

            sales.Add(sale);
            await unitOfWork.SaveChangesAsync(token);

            // The stock ledger references the sale, so it is written once the sale has an id.
            foreach (var item in sale.Items)
            {
                stockAdjustments.Add(new StockAdjustment(
                    productsById[item.ProductId], StockAdjustmentType.Sale, -item.Quantity,
                    stockBefore[item.ProductId], currentUser.UserId, $"Sale {sale.InvoiceNumber}", sale.Id));
            }

            if (WhatsAppTemplates.ForSale(store, sale, clock.UtcNow) is { } thankYou)
                messages.Add(thankYou);

            await unitOfWork.SaveChangesAsync(token);
            return sale.Id;
        }, ct);

        dispatchSignal.Notify();
        return await GetAsync(saleId, ct);
    }

    public Task<PagedResult<SaleSummaryDto>> ListAsync(SaleQuery query, CancellationToken ct)
    {
        var cashierId = currentUser.IsAdmin ? query.CashierId : currentUser.UserId;
        var filter = new SaleFilter(query, query.Search, storeClock.ToUtcRange(query.From, query.To), cashierId);
        return sales.ListAsync(filter, SaleMappings.SummaryProjection, ct);
    }

    public async Task<SaleDto> GetAsync(int id, CancellationToken ct) =>
        await sales.FindAsync(id, SaleMappings.Projection, ct) ?? throw NotFoundException.For("Sale", id);

    public async Task<SaleDto> GetByInvoiceAsync(string invoiceNumber, CancellationToken ct) =>
        await sales.FindByInvoiceAsync(invoiceNumber.Trim().ToUpperInvariant(), SaleMappings.Projection, ct)
        ?? throw NotFoundException.For("Invoice", invoiceNumber);

    public async Task<ReceiptDto> GetReceiptAsync(int id, CancellationToken ct)
    {
        var sale = await GetAsync(id, ct);
        var entity = await settings.GetAsync(ct);
        var store = entity.ToDto(storeClock.TimeZoneId, await settings.GetImageVersionsAsync(ct));
        return new ReceiptDto(store, sale, BuildQrContent(sale));
    }

    private async Task<Customer?> FindOrCreateCustomerAsync(CreateSaleRequest request, StoreSettings store, CancellationToken ct)
    {
        var phone = PhoneNumber.Normalize(request.CustomerPhone, store.DefaultCountryCode);
        if (phone is null)
            return null;

        var customer = await customers.GetByPhoneAsync(phone, ct);
        if (customer is null)
        {
            customer = new Customer(request.CustomerName, phone);
            customers.Add(customer);
        }
        else
        {
            customer.RememberName(request.CustomerName);
        }

        return customer;
    }

    private async Task<DiscountCode?> FindDiscountCodeAsync(string? code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var normalized = DiscountCode.NormalizeCode(code);
        return await discountCodes.GetByCodeAsync(normalized, ct)
            ?? throw new NotFoundException($"Discount code '{normalized}' does not exist.");
    }

    /// <summary>The receipt QR holds only the invoice number, so scanning it fills the returns search.</summary>
    private static string BuildQrContent(SaleDto sale) => sale.InvoiceNumber;
}

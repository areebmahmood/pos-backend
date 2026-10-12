using Microsoft.EntityFrameworkCore;
using Pos.Application.Features.Expenses;
using Pos.Application.Features.Products;
using Pos.Application.Features.Registers;
using Pos.Application.Features.Returns;
using Pos.Application.Features.Sales;
using Pos.Domain.Enums;
using Pos.Domain.Exceptions;
using Pos.Tests.Infrastructure;

namespace Pos.Tests.Application;

public sealed class CheckoutFlowTests : IAsyncLifetime
{
    private readonly PosTestHost _host = new();
    private ProductDto _rice = default!;
    private ProductDto _soap = default!;

    public async Task InitializeAsync()
    {
        var admin = await _host.AddUserAsync("admin", UserRole.Admin);
        _host.SignInAs(admin);

        _rice = await _host.RunAsync<IProductService, ProductDto>(s => s.CreateAsync(
            new CreateProductRequest("Rice 1kg", null, "Long grain", null, 280, 350, 320, 50, 5), default));
        _soap = await _host.RunAsync<IProductService, ProductDto>(s => s.CreateAsync(
            new CreateProductRequest("Soap", "SOAP-1", null, null, 90, 120, null, 10, 2), default));
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public void Product_gets_a_valid_generated_ean13_barcode()
    {
        Assert.Equal(13, _rice.Barcode.Length);
        Assert.StartsWith("20", _rice.Barcode);
        Assert.Equal(320, _rice.EffectivePrice);
        Assert.Equal(8.57m, _rice.DiscountPercent);
    }

    [Fact]
    public async Task Billing_requires_an_open_register()
    {
        var error = await Assert.ThrowsAsync<DomainException>(() => CheckoutAsync(new SaleLineRequest(_rice.Id, 1)));
        Assert.Contains("Open your register", error.Message);
    }

    [Fact]
    public async Task Sale_applies_product_and_bill_discounts_and_decrements_stock()
    {
        await OpenRegisterAsync(1000);

        var sale = await CheckoutAsync(
            billDiscount: 40, amountPaid: 2000,
            lines: [new SaleLineRequest(_rice.Id, 2), new SaleLineRequest(_soap.Id, 3), new SaleLineRequest(_rice.Id, 1)]);

        Assert.Equal(2, sale.Items.Count);                 // duplicate lines are merged
        Assert.Equal(3 * 350 + 3 * 120, sale.Subtotal);    // 1410 at list price
        Assert.Equal(3 * 30, sale.ItemDiscount);           // rice is discounted by 30 per unit
        Assert.Equal(40, sale.BillDiscount);
        Assert.Equal(1410 - 90 - 40, sale.Total);
        Assert.Equal(2000 - sale.Total, sale.Change);
        Assert.StartsWith("INV-261010-", sale.InvoiceNumber);

        var stock = await _host.QueryDbAsync(db => db.Products.Select(p => new { p.Id, p.StockQuantity }).ToListAsync());
        Assert.Equal(47, stock.Single(p => p.Id == _rice.Id).StockQuantity);
        Assert.Equal(7, stock.Single(p => p.Id == _soap.Id).StockQuantity);

        var ledger = await _host.QueryDbAsync(db => db.StockAdjustments.Where(a => a.Type == StockAdjustmentType.Sale).ToListAsync());
        Assert.All(ledger, entry => Assert.Equal(sale.Id, entry.ReferenceId));
        Assert.Equal(2, ledger.Count);
    }

    [Fact]
    public async Task Line_discount_is_per_unit_on_top_of_the_product_discount_and_refunded_pro_rata()
    {
        await OpenRegisterAsync(0);

        // Rice: 3 x 320 = 960, minus a 20-per-unit discount (60) = 900. Soap: 2 x 120 = 240, no line discount.
        var sale = await CheckoutAsync(
            billDiscount: 0, amountPaid: 2000,
            lines: [new SaleLineRequest(_rice.Id, 3, 20), new SaleLineRequest(_soap.Id, 2)]);

        var rice = sale.Items.Single(i => i.ProductId == _rice.Id);
        Assert.Equal(20, rice.UnitDiscount);
        Assert.Equal(60, rice.ManualDiscount);
        Assert.Equal(3 * 30 + 60, rice.LineDiscount);
        Assert.Equal(900, rice.LineTotal);
        Assert.Equal(3 * 350 + 2 * 120, sale.Subtotal);
        Assert.Equal(150, sale.ItemDiscount);
        Assert.Equal(1140, sale.Total);

        var saleReturn = await _host.RunAsync<IReturnService, SaleReturnDto>(s => s.CreateAsync(
            new CreateReturnRequest(sale.Id, null, [new ReturnLineRequest(rice.Id, 1)]), default));
        Assert.Equal(300, saleReturn.RefundAmount);
    }

    [Fact]
    public async Task Line_discount_cannot_exceed_the_unit_price()
    {
        await OpenRegisterAsync(0);

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            CheckoutAsync(billDiscount: 0, amountPaid: 1000, lines: [new SaleLineRequest(_soap.Id, 1, 121)]));
        Assert.Contains("cannot exceed its price", error.Message);
    }

    [Fact]
    public async Task Selling_more_than_in_stock_fails_without_saving_anything()
    {
        await OpenRegisterAsync(0);

        await Assert.ThrowsAsync<DomainException>(() => CheckoutAsync(new SaleLineRequest(_soap.Id, 11)));

        Assert.Equal(0, await _host.QueryDbAsync(db => db.Sales.CountAsync()));
        Assert.Equal(10, await _host.QueryDbAsync(db => db.Products.Where(p => p.Id == _soap.Id).Select(p => p.StockQuantity).SingleAsync()));
    }

    [Fact]
    public async Task Cash_payment_must_cover_the_total()
    {
        await OpenRegisterAsync(0);

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            CheckoutAsync(billDiscount: 0, amountPaid: 100, lines: [new SaleLineRequest(_soap.Id, 1)]));
        Assert.Contains("less than the total", error.Message);
    }

    [Fact]
    public async Task Return_refunds_the_paid_price_restocks_and_feeds_register_close()
    {
        var register = await OpenRegisterAsync(1000);
        var sale = await CheckoutAsync(
            billDiscount: 64, amountPaid: 640,
            lines: [new SaleLineRequest(_rice.Id, 2)]);  // 2 x 320 = 640, bill discount 10% => total 576
        Assert.Equal(576, sale.Total);

        var saleReturn = await _host.RunAsync<IReturnService, SaleReturnDto>(s => s.CreateAsync(
            new CreateReturnRequest(sale.Id, "Damaged", [new ReturnLineRequest(sale.Items[0].Id, 1)]), default));
        Assert.Equal(288, saleReturn.RefundAmount);

        var updated = await _host.RunAsync<ISaleService, SaleDto>(s => s.GetAsync(sale.Id, default));
        Assert.Equal(SaleStatus.PartiallyReturned, updated.Status);
        Assert.Equal(1, updated.Items[0].ReturnedQuantity);

        await Assert.ThrowsAsync<DomainException>(() => _host.RunAsync<IReturnService, SaleReturnDto>(s => s.CreateAsync(
            new CreateReturnRequest(sale.Id, null, [new ReturnLineRequest(sale.Items[0].Id, 2)]), default)));

        await _host.RunAsync<IExpenseService, ExpenseDto>(s => s.CreateAsync(new CreateExpenseRequest("Tea", "Staff", 50, null), default));

        var closed = await _host.RunAsync<IRegisterService, RegisterSessionDto>(s =>
            s.CloseAsync(register.Id, new CloseRegisterRequest(1230, "End of day"), default));

        Assert.Equal(RegisterStatus.Closed, closed.Status);
        Assert.Equal(576, closed.CashSales);
        Assert.Equal(288, closed.CashRefunds);
        Assert.Equal(50, closed.Expenses);
        Assert.Equal(1000 + 576 - 288 - 50, closed.ExpectedCash);
        Assert.Equal(1230 - 1238, closed.Difference);
        Assert.Equal(49, await _host.QueryDbAsync(db => db.Products.Where(p => p.Id == _rice.Id).Select(p => p.StockQuantity).SingleAsync()));
    }

    [Fact]
    public async Task Only_one_register_can_be_open_per_user()
    {
        await OpenRegisterAsync(0);
        await Assert.ThrowsAsync<Pos.Application.Common.Exceptions.ConflictException>(() => OpenRegisterAsync(0));
    }

    private Task<RegisterSessionDto> OpenRegisterAsync(decimal cash) =>
        _host.RunAsync<IRegisterService, RegisterSessionDto>(s => s.OpenAsync(new OpenRegisterRequest(cash, null), default));

    private Task<SaleDto> CheckoutAsync(params SaleLineRequest[] lines) => CheckoutAsync(0, 100_000, lines);

    private Task<SaleDto> CheckoutAsync(decimal billDiscount, decimal amountPaid, SaleLineRequest[] lines) =>
        _host.RunAsync<ISaleService, SaleDto>(s => s.CreateAsync(
            new CreateSaleRequest(lines, PaymentMethod.Cash, amountPaid, billDiscount, null, null), default));
}

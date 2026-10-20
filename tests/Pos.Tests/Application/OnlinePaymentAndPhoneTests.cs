using Pos.Application.Common.Exceptions;
using Pos.Application.Features.Customers;
using Pos.Application.Features.Products;
using Pos.Application.Features.Registers;
using Pos.Application.Features.Reports;
using Pos.Application.Features.Sales;
using Pos.Domain.Common;
using Pos.Domain.Enums;
using Pos.Tests.Infrastructure;

namespace Pos.Tests.Application;

public sealed class OnlinePaymentAndPhoneTests : IAsyncLifetime
{
    private readonly PosTestHost _host = new();
    private ProductDto _shirt = default!;

    public async Task InitializeAsync()
    {
        var admin = await _host.AddUserAsync("admin", UserRole.Admin);
        _host.SignInAs(admin);
        _shirt = await _host.RunAsync<IProductService, ProductDto>(s => s.CreateAsync(
            new CreateProductRequest("Shirt", null, null, null, 600, 1000, null, 100, 5), default));
        await _host.RunAsync<IRegisterService, RegisterSessionDto>(s => s.OpenAsync(new OpenRegisterRequest(0, null), default));
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Theory]
    [InlineData("03001234567", true)]
    [InlineData("0300-1234567", true)]
    [InlineData("0300 1234567", true)]
    [InlineData("+923001234567", true)]
    [InlineData("+92 300 1234567", true)]
    [InlineData("00923001234567", true)]
    [InlineData("923001234567", true)]
    [InlineData("0300123456", false)]    // 10 digits
    [InlineData("030012345678", false)]  // 12 digits
    [InlineData("04235761234", false)]   // landline, not 03
    [InlineData("+92300123456", false)]  // +92 with 9 digits
    [InlineData("0300123456a", false)]
    [InlineData("03121234567", true)]    // Zong
    [InlineData("03211234567", true)]    // Jazz (ex-Warid)
    [InlineData("03331234567", true)]    // Ufone
    [InlineData("03451234567", true)]    // Telenor
    [InlineData("03551234567", true)]    // SCO
    [InlineData("+923551234567", true)]
    [InlineData("03501234567", false)]   // no network uses 0350
    [InlineData("03601234567", false)]   // no network uses 036x
    [InlineData("03991234567", false)]
    [InlineData("+923901234567", false)]
    [InlineData("", false)]
    public void Mobile_numbers_must_be_pakistani_mobiles(string raw, bool valid) =>
        Assert.Equal(valid, PhoneNumber.IsValidMobile(raw));

    [Fact]
    public async Task Online_sale_keeps_its_payment_type_and_counts_in_register_and_report_totals()
    {
        var sale = await CheckoutAsync(PaymentMethod.Online, OnlinePaymentType.EasyPaisa);
        await CheckoutAsync(PaymentMethod.Online, OnlinePaymentType.JazzCash);
        await CheckoutAsync(PaymentMethod.Cash, null);

        Assert.Equal(PaymentMethod.Online, sale.PaymentMethod);
        Assert.Equal(OnlinePaymentType.EasyPaisa, sale.OnlinePaymentType);
        Assert.Equal(sale.Total, sale.AmountPaid);
        Assert.Equal(0, sale.Change);

        var summary = Assert.Single((await _host.RunAsync<ISaleService, Pos.Application.Common.Models.PagedResult<SaleSummaryDto>>(s =>
            s.ListAsync(new SaleQuery { Search = sale.InvoiceNumber }, default))).Items);
        Assert.Equal(OnlinePaymentType.EasyPaisa, summary.OnlinePaymentType);

        var register = await _host.RunAsync<IRegisterService, RegisterSessionDto?>(s => s.GetCurrentAsync(default));
        Assert.Equal(2000, register!.OnlineSales);
        Assert.Equal(1000, register.CashSales);
        Assert.Equal(1000, register.ExpectedCash);

        var report = await _host.RunAsync<IReportService, SalesReportDto>(s => s.GetSalesReportAsync(
            new SalesReportQuery { Period = ReportPeriod.Daily, Date = DateOnly.FromDateTime(_host.Clock.UtcNow.AddHours(5)) }, default));
        Assert.Equal(2000, report.Summary.OnlineSales);
        Assert.Equal(1000, report.Summary.JazzCashSales);
        Assert.Equal(1000, report.Summary.EasyPaisaSales);
        Assert.Equal(0, report.Summary.BankTransferSales);
    }

    [Fact]
    public async Task Online_sale_needs_a_payment_type_and_other_methods_ignore_it()
    {
        var error = await Assert.ThrowsAsync<ValidationException>(() => CheckoutAsync(PaymentMethod.Online, null));
        Assert.Contains("onlinePaymentType", error.Errors.Keys);

        var card = await CheckoutAsync(PaymentMethod.Card, OnlinePaymentType.BankTransfer);
        Assert.Null(card.OnlinePaymentType);
    }

    [Fact]
    public async Task Invalid_phone_numbers_are_rejected_on_bills_and_customers()
    {
        var bill = await Assert.ThrowsAsync<ValidationException>(() => CheckoutAsync(PaymentMethod.Cash, null, "12345"));
        Assert.Contains("customerPhone", bill.Errors.Keys);

        var customer = await Assert.ThrowsAsync<ValidationException>(() => _host.RunAsync<ICustomerService, CustomerDto>(s =>
            s.CreateAsync(new SaveCustomerRequest("A", "030012345678"), default)));
        Assert.Contains("phone", customer.Errors.Keys);

        var ok = await CheckoutAsync(PaymentMethod.Cash, null, "0300-1234567");
        Assert.NotNull(ok.CustomerId);
    }

    private Task<SaleDto> CheckoutAsync(PaymentMethod method, OnlinePaymentType? type, string? phone = null) =>
        _host.RunAsync<ISaleService, SaleDto>(s => s.CreateAsync(
            new CreateSaleRequest([new SaleLineRequest(_shirt.Id, 1)], method, 1000, 0, null, phone, null, type), default));
}

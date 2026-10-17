using Microsoft.EntityFrameworkCore;
using Pos.Application.Common.Abstractions.Services;
using Pos.Application.Common.Exceptions;
using Pos.Application.Features.Customers;
using Pos.Application.Features.DiscountCodes;
using Pos.Application.Features.Messaging;
using Pos.Application.Features.Products;
using Pos.Application.Features.Registers;
using Pos.Application.Features.Returns;
using Pos.Application.Features.Sales;
using Pos.Application.Features.Settings;
using Pos.Domain.Common;
using Pos.Domain.Enums;
using Pos.Domain.Exceptions;
using Pos.Tests.Infrastructure;

namespace Pos.Tests.Application;

public sealed class CustomersAndMessagingTests : IAsyncLifetime
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
    [InlineData("0300-1234567", "923001234567")]
    [InlineData("+92 300 1234567", "923001234567")]
    [InlineData("00923001234567", "923001234567")]
    [InlineData("3001234567", "923001234567")]
    [InlineData("923001234567", "923001234567")]
    [InlineData("12345", null)]
    [InlineData("", null)]
    public void Phone_numbers_are_normalized_to_international_digits(string raw, string? expected) =>
        Assert.Equal(expected, PhoneNumber.Normalize(raw, "92"));

    [Fact]
    public async Task Receipt_qr_contains_only_the_invoice_number()
    {
        var sale = await CheckoutAsync(phone: null);
        var receipt = await _host.RunAsync<ISaleService, ReceiptDto>(s => s.GetReceiptAsync(sale.Id, default));
        Assert.Equal(sale.InvoiceNumber, receipt.QrContent);
    }

    [Fact]
    public async Task Billing_with_a_phone_creates_one_customer_and_queues_a_thank_you_message()
    {
        await EnableWhatsAppAsync();

        var first = await CheckoutAsync(phone: "0300 1234567", name: "Ayesha");
        var second = await CheckoutAsync(phone: "+923001234567", name: null);
        Assert.NotNull(first.CustomerId);
        Assert.Equal(first.CustomerId, second.CustomerId);

        var customers = await _host.RunAsync<ICustomerService, Pos.Application.Common.Models.PagedResult<CustomerDto>>(
            s => s.ListAsync(new CustomerQuery(), default));
        var customer = Assert.Single(customers.Items);
        Assert.Equal("923001234567", customer.Phone);
        Assert.Equal("Ayesha", customer.Name);
        Assert.Equal(2, customer.PurchaseCount);
        Assert.Equal(2000, customer.TotalSpent);

        var sent = await _host.RunAsync<IMessagingService, int>(s => s.DispatchPendingAsync(default));
        Assert.Equal(2, sent);
        Assert.All(_host.WhatsApp.Sent, m => Assert.Equal("thank_you", m.Template));
        Assert.Equal(["Ayesha", first.InvoiceNumber, "Rs 1,000"], _host.WhatsApp.Sent[0].Parameters);
    }

    [Fact]
    public async Task No_message_is_queued_while_whatsapp_is_off()
    {
        var sale = await CheckoutAsync(phone: "03001234567");
        Assert.NotNull(sale.CustomerId);
        Assert.Equal(0, await _host.QueryDbAsync(db => db.OutgoingMessages.CountAsync()));
    }

    [Fact]
    public async Task Promotions_skip_customers_who_opted_out_and_record_failures()
    {
        await EnableWhatsAppAsync();
        var keen = await CreateCustomerAsync("Bilal", "03011111111");
        var optedOut = await CreateCustomerAsync("Sara", "03022222222");
        await _host.RunAsync<ICustomerService, CustomerDto>(s => s.UpdateAsync(
            optedOut.Id, new SaveCustomerRequest("Sara", "03022222222", OptedOut: true), default));

        var promotion = await _host.RunAsync<IMessagingService, PromotionDto>(s => s.SendPromotionAsync(
            new SendPromotionRequest("Eid sale!\nUse code EID20 for 20% off.", null), default));
        Assert.Equal(1, promotion.RecipientCount);
        Assert.Equal(1, promotion.PendingCount);

        _host.WhatsApp.FailWith = new WhatsAppSendResult(false, "Template not found", Retryable: false);
        await _host.RunAsync<IMessagingService, int>(s => s.DispatchPendingAsync(default));

        var messages = await _host.RunAsync<IMessagingService, Pos.Application.Common.Models.PagedResult<OutgoingMessageDto>>(
            s => s.ListMessagesAsync(new MessageQuery { PromotionId = promotion.Id }, default));
        var message = Assert.Single(messages.Items);
        Assert.Equal(keen.Phone, message.Phone);
        Assert.Equal(MessageStatus.Failed, message.Status);
        Assert.Equal("Template not found", message.Error);
    }

    [Fact]
    public async Task Promotion_text_is_sent_on_one_line()
    {
        await EnableWhatsAppAsync();
        await CreateCustomerAsync(null, "03011111111");
        await _host.RunAsync<IMessagingService, PromotionDto>(s => s.SendPromotionAsync(
            new SendPromotionRequest("Eid sale!\n\nUse code EID20.", null), default));
        await _host.RunAsync<IMessagingService, int>(s => s.DispatchPendingAsync(default));

        var sent = Assert.Single(_host.WhatsApp.Sent);
        Assert.Equal(["Customer", "Eid sale! Use code EID20."], sent.Parameters);
    }

    [Fact]
    public async Task Duplicate_customer_phone_is_rejected()
    {
        await CreateCustomerAsync("A", "03011111111");
        await Assert.ThrowsAsync<ConflictException>(() => CreateCustomerAsync("B", "+92 301 1111111"));
    }

    [Fact]
    public async Task Discount_code_applies_after_the_bill_discount_counts_its_use_and_is_refunded_pro_rata()
    {
        await CreateCodeAsync(new SaveDiscountCodeRequest("eid20", null, DiscountType.Percentage, 20, 500, 300, null, null, 1));

        var check = await _host.RunAsync<IDiscountCodeService, DiscountCodeCheckDto>(s => s.CheckAsync("EID20", 1900, default));
        Assert.Equal(300, check.Discount); // 20% of 1900 = 380, capped at 300

        var sale = await CheckoutAsync(phone: null, quantity: 2, billDiscount: 100, discountCode: " eid20 ");
        Assert.Equal("EID20", sale.DiscountCode);
        Assert.Equal(300, sale.CodeDiscount);
        Assert.Equal(400, sale.BillDiscount);
        Assert.Equal(1600, sale.Total);

        var error = await Assert.ThrowsAsync<DomainException>(() => CheckoutAsync(phone: null, discountCode: "EID20"));
        Assert.Contains("usage limit", error.Message);

        var refund = await _host.RunAsync<IReturnService, SaleReturnDto>(s => s.CreateAsync(
            new CreateReturnRequest(sale.Id, null, [new ReturnLineRequest(sale.Items.Single().Id, 1)]), default));
        Assert.Equal(800, refund.RefundAmount);
    }

    [Fact]
    public async Task Discount_code_rules_are_enforced()
    {
        await CreateCodeAsync(new SaveDiscountCodeRequest("BIG", null, DiscountType.Amount, 200, 5000, null, null, null, null));
        await CreateCodeAsync(new SaveDiscountCodeRequest("OFF", null, DiscountType.Amount, 200, null, null, null, null, null, IsActive: false));

        Assert.Contains("at least", (await Assert.ThrowsAsync<DomainException>(() => CheckoutAsync(phone: null, discountCode: "BIG"))).Message);
        Assert.Contains("not active", (await Assert.ThrowsAsync<DomainException>(() => CheckoutAsync(phone: null, discountCode: "OFF"))).Message);
        await Assert.ThrowsAsync<NotFoundException>(() => CheckoutAsync(phone: null, discountCode: "NOPE"));
    }

    [Fact]
    public async Task Whatsapp_access_token_is_kept_when_left_empty_and_never_returned()
    {
        await EnableWhatsAppAsync();
        var saved = await _host.RunAsync<ISettingsService, WhatsAppSettingsDto>(s => s.UpdateWhatsAppAsync(
            new UpdateWhatsAppSettingsRequest(true, "123456", null, "thank_you", "promo", "en", "92"), default));
        Assert.True(saved.HasAccessToken);
        Assert.True(saved.Enabled);
    }

    private Task EnableWhatsAppAsync() =>
        _host.RunAsync<ISettingsService, WhatsAppSettingsDto>(s => s.UpdateWhatsAppAsync(
            new UpdateWhatsAppSettingsRequest(true, "123456", "token", "thank_you", "promo", "en", "92"), default));

    private Task<CustomerDto> CreateCustomerAsync(string? name, string phone) =>
        _host.RunAsync<ICustomerService, CustomerDto>(s => s.CreateAsync(new SaveCustomerRequest(name, phone), default));

    private Task<DiscountCodeDto> CreateCodeAsync(SaveDiscountCodeRequest request) =>
        _host.RunAsync<IDiscountCodeService, DiscountCodeDto>(s => s.CreateAsync(request, default));

    private Task<SaleDto> CheckoutAsync(
        string? phone, string? name = null, decimal quantity = 1, decimal billDiscount = 0, string? discountCode = null) =>
        _host.RunAsync<ISaleService, SaleDto>(s => s.CreateAsync(
            new CreateSaleRequest([new SaleLineRequest(_shirt.Id, quantity)], PaymentMethod.Card, 0, billDiscount, name, phone, discountCode),
            default));
}

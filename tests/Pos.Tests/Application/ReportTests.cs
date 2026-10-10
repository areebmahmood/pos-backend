using Pos.Application.Features.Dashboard;
using Pos.Application.Features.Products;
using Pos.Application.Features.Registers;
using Pos.Application.Features.Reports;
using Pos.Application.Features.Sales;
using Pos.Domain.Enums;
using Pos.Tests.Infrastructure;

namespace Pos.Tests.Application;

public sealed class ReportTests : IAsyncLifetime
{
    private readonly PosTestHost _host = new();

    public async Task InitializeAsync()
    {
        var admin = await _host.AddUserAsync("admin", UserRole.Admin);
        _host.SignInAs(admin);

        var product = await _host.RunAsync<IProductService, ProductDto>(s => s.CreateAsync(
            new CreateProductRequest("Cola", null, null, null, 100, 150, null, 100, 5), default));
        await _host.RunAsync<IRegisterService, RegisterSessionDto>(s => s.OpenAsync(new OpenRegisterRequest(0, null), default));

        // 2026-10-10 12:00 and 18:30 Pakistan time, plus one sale on 2026-10-03.
        foreach (var (utc, quantity, method) in new[]
                 {
                     (new DateTime(2026, 10, 10, 7, 0, 0, DateTimeKind.Utc), 2m, PaymentMethod.Cash),
                     (new DateTime(2026, 10, 10, 13, 30, 0, DateTimeKind.Utc), 1m, PaymentMethod.Card),
                     (new DateTime(2026, 10, 3, 5, 0, 0, DateTimeKind.Utc), 4m, PaymentMethod.Cash)
                 })
        {
            _host.Clock.UtcNow = utc;
            await _host.RunAsync<ISaleService, SaleDto>(s => s.CreateAsync(
                new CreateSaleRequest([new SaleLineRequest(product.Id, quantity)], method, 1000, 0, null, null), default));
        }

        _host.Clock.UtcNow = new DateTime(2026, 10, 10, 15, 0, 0, DateTimeKind.Utc);
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task Daily_report_groups_sales_by_local_hour()
    {
        var report = await GetReportAsync(ReportPeriod.Daily, new DateOnly(2026, 10, 10));

        Assert.Equal(24, report.Buckets.Count);
        Assert.Equal(2, report.Summary.SaleCount);
        Assert.Equal(450, report.Summary.NetSales);
        Assert.Equal(300, report.Summary.CashSales);
        Assert.Equal(150, report.Summary.CardSales);
        Assert.Equal(300, report.Summary.CostOfGoods);
        Assert.Equal(150, report.Summary.GrossProfit);
        Assert.Equal(300, report.Buckets.Single(b => b.Label == "12:00").NetSales);
        Assert.Equal(150, report.Buckets.Single(b => b.Label == "18:00").NetSales);
    }

    [Fact]
    public async Task Weekly_monthly_and_yearly_reports_cover_the_right_ranges()
    {
        var weekly = await GetReportAsync(ReportPeriod.Weekly, new DateOnly(2026, 10, 10));
        Assert.Equal(new DateOnly(2026, 10, 5), weekly.From);   // Monday
        Assert.Equal(new DateOnly(2026, 10, 11), weekly.To);
        Assert.Equal(7, weekly.Buckets.Count);
        Assert.Equal(2, weekly.Summary.SaleCount);

        var monthly = await GetReportAsync(ReportPeriod.Monthly, new DateOnly(2026, 10, 10));
        Assert.Equal(31, monthly.Buckets.Count);
        Assert.Equal(3, monthly.Summary.SaleCount);
        Assert.Equal(1050, monthly.Summary.NetSales);
        Assert.Equal(7, monthly.Summary.ItemsSold);
        Assert.Equal("Cola", Assert.Single(monthly.TopProducts).ProductName);

        var yearly = await GetReportAsync(ReportPeriod.Yearly, new DateOnly(2026, 10, 10));
        Assert.Equal(12, yearly.Buckets.Count);
        Assert.Equal(1050, yearly.Buckets.Single(b => b.Label == "Oct 2026").NetSales);
    }

    [Fact]
    public async Task Admin_dashboard_shows_today_and_trend()
    {
        var dashboard = await _host.RunAsync<IDashboardService, AdminDashboardDto>(s => s.GetAdminDashboardAsync(default));

        Assert.Equal(450, dashboard.Today.NetSales);
        Assert.Equal(1050, dashboard.Month.NetSales);
        Assert.Equal(7, dashboard.Last7Days.Count);
        Assert.Equal(450, dashboard.Last7Days[^1].NetSales);
        Assert.Single(dashboard.OpenRegisters);
        Assert.Equal(3, dashboard.RecentSales.Count);
    }

    private Task<SalesReportDto> GetReportAsync(ReportPeriod period, DateOnly date) =>
        _host.RunAsync<IReportService, SalesReportDto>(s =>
            s.GetSalesReportAsync(new SalesReportQuery { Period = period, Date = date }, default));
}

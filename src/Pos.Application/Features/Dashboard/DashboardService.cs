using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Abstractions.Services;
using Pos.Application.Features.Products;
using Pos.Application.Features.Registers;
using Pos.Application.Features.Sales;
using Pos.Domain.Common;

namespace Pos.Application.Features.Dashboard;

public sealed class DashboardService(
    IReportQueries reports,
    IProductRepository products,
    ISaleRepository sales,
    IRegisterService registerService,
    ICurrentUser currentUser,
    IStoreClock storeClock) : IDashboardService
{
    private const int ListSize = 10;
    private const int TrendDays = 7;

    public async Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken ct)
    {
        var today = storeClock.Today;
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var todaySummary = await GetSummaryAsync(today, today, null, ct);
        var monthSummary = await GetSummaryAsync(monthStart, today, null, ct);
        var totalProducts = await products.CountAsync(lowStockOnly: false, ct);
        var lowStockCount = await products.CountAsync(lowStockOnly: true, ct);
        var lowStock = await products.ListAsync(
            new ProductQuery { LowStock = true, IncludeInactive = false, PageSize = ListSize }, ProductMappings.Projection, ct);
        var openRegisters = await registerService.ListOpenAsync(ct);
        var recentSales = await sales.ListRecentAsync(null, ListSize, SaleMappings.SummaryProjection, ct);
        var trend = await GetDailyTrendAsync(today, ct);

        return new AdminDashboardDto(
            todaySummary, monthSummary, totalProducts, lowStockCount, lowStock.Items, openRegisters, recentSales, trend);
    }

    public async Task<SalesmanDashboardDto> GetSalesmanDashboardAsync(CancellationToken ct)
    {
        var today = storeClock.Today;
        var register = await registerService.GetCurrentAsync(ct);
        var summary = await GetSummaryAsync(today, today, currentUser.UserId, ct);
        var recentSales = await sales.ListRecentAsync(currentUser.UserId, ListSize, SaleMappings.SummaryProjection, ct);

        return new SalesmanDashboardDto(register, summary, recentSales);
    }

    private async Task<PeriodSummaryDto> GetSummaryAsync(DateOnly from, DateOnly to, int? userId, CancellationToken ct)
    {
        var totals = await reports.GetPeriodTotalsAsync(storeClock.ToUtcRange(from, to), userId, ct);
        return new PeriodSummaryDto(
            Money.Round(totals.NetSales), totals.SaleCount, Money.Round(totals.Returns), Money.Round(totals.Expenses));
    }

    private async Task<IReadOnlyList<DailySalesDto>> GetDailyTrendAsync(DateOnly today, CancellationToken ct)
    {
        var from = today.AddDays(-(TrendDays - 1));
        var range = storeClock.ToUtcRange(from, today);
        var hourly = await reports.GetSalesByHourAsync(range, storeClock.OffsetMinutes(range.FromUtc), null, ct);
        var byDay = hourly
            .GroupBy(h => DateOnly.FromDateTime(h.LocalHour))
            .ToDictionary(g => g.Key, g => (Total: g.Sum(h => h.Total), Count: g.Sum(h => h.SaleCount)));

        return Enumerable.Range(0, TrendDays)
            .Select(i => from.AddDays(i))
            .Select(day =>
            {
                var (total, count) = byDay.GetValueOrDefault(day);
                return new DailySalesDto(day, Money.Round(total), count);
            })
            .ToList();
    }
}

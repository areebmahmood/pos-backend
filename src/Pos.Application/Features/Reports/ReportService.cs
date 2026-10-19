using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Abstractions.Services;
using Pos.Domain.Common;

namespace Pos.Application.Features.Reports;

public sealed class ReportService(IReportQueries queries, IStoreClock storeClock) : IReportService
{
    private const int TopProductCount = 10;

    public async Task<SalesReportDto> GetSalesReportAsync(SalesReportQuery query, CancellationToken ct)
    {
        var (from, to) = ReportPeriodCalculator.GetRange(query.Period, query.Date ?? storeClock.Today);
        var range = storeClock.ToUtcRange(from, to);
        var offset = storeClock.OffsetMinutes(range.FromUtc);

        var sales = await queries.GetSalesByHourAsync(range, offset, null, ct);
        var returns = await queries.GetReturnsByHourAsync(range, offset, null, ct);
        var expenses = await queries.GetExpensesTotalAsync(range, null, ct);
        var items = await queries.GetItemTotalsAsync(range, ct);
        var topProducts = await queries.GetTopProductsAsync(range, TopProductCount, ct);
        var cashiers = await queries.GetCashierTotalsAsync(range, ct);

        var summary = BuildSummary(sales, returns, items, expenses);
        var buckets = BuildBuckets(query.Period, from, to, sales, returns);

        return new SalesReportDto(
            query.Period,
            from,
            to,
            summary,
            buckets,
            topProducts.Select(p => new TopProductDto(p.ProductId, p.ProductName, p.Quantity, p.Revenue)).ToList(),
            cashiers.Select(c => new CashierSalesDto(c.UserId, c.UserName, c.SaleCount, c.Total)).ToList());
    }

    private static SalesReportSummary BuildSummary(
        IReadOnlyList<SalesHourAggregate> sales,
        IReadOnlyList<ReturnsHourAggregate> returns,
        ItemTotals items,
        decimal expenses)
    {
        var saleCount = sales.Sum(s => s.SaleCount);
        var netSales = sales.Sum(s => s.Total);
        var refunds = returns.Sum(r => r.RefundAmount);
        var netRevenue = netSales - refunds;
        var costOfGoods = items.CostOfGoods - returns.Sum(r => r.CostAmount);
        var grossProfit = netRevenue - costOfGoods;

        return new SalesReportSummary(
            SaleCount: saleCount,
            ReturnCount: returns.Sum(r => r.ReturnCount),
            ItemsSold: items.ItemsSold,
            GrossSales: Money.Round(sales.Sum(s => s.Subtotal)),
            ItemDiscounts: Money.Round(sales.Sum(s => s.ItemDiscount)),
            BillDiscounts: Money.Round(sales.Sum(s => s.BillDiscount)),
            NetSales: Money.Round(netSales),
            Returns: Money.Round(refunds),
            NetRevenue: Money.Round(netRevenue),
            CostOfGoods: Money.Round(costOfGoods),
            GrossProfit: Money.Round(grossProfit),
            Expenses: Money.Round(expenses),
            NetProfit: Money.Round(grossProfit - expenses),
            CashSales: Money.Round(sales.Sum(s => s.CashTotal)),
            CardSales: Money.Round(sales.Sum(s => s.CardTotal)),
            OnlineSales: Money.Round(sales.Sum(s => s.OnlineTotal)),
            JazzCashSales: Money.Round(sales.Sum(s => s.JazzCashTotal)),
            EasyPaisaSales: Money.Round(sales.Sum(s => s.EasyPaisaTotal)),
            BankTransferSales: Money.Round(sales.Sum(s => s.BankTransferTotal)),
            AverageSale: saleCount == 0 ? 0 : Money.Round(netSales / saleCount));
    }

    private static List<SalesReportBucket> BuildBuckets(
        ReportPeriod period,
        DateOnly from,
        DateOnly to,
        IReadOnlyList<SalesHourAggregate> sales,
        IReadOnlyList<ReturnsHourAggregate> returns)
    {
        var salesByBucket = sales
            .GroupBy(s => ReportPeriodCalculator.BucketStart(period, s.LocalHour))
            .ToDictionary(g => g.Key, g => (Count: g.Sum(s => s.SaleCount), Total: g.Sum(s => s.Total)));
        var returnsByBucket = returns
            .GroupBy(r => ReportPeriodCalculator.BucketStart(period, r.LocalHour))
            .ToDictionary(g => g.Key, g => g.Sum(r => r.RefundAmount));

        return ReportPeriodCalculator.EnumerateBuckets(period, from, to)
            .Select(start =>
            {
                var (count, total) = salesByBucket.GetValueOrDefault(start);
                var refunds = returnsByBucket.GetValueOrDefault(start);
                return new SalesReportBucket(
                    ReportPeriodCalculator.Label(period, start),
                    start,
                    count,
                    Money.Round(total),
                    Money.Round(refunds),
                    Money.Round(total - refunds));
            })
            .ToList();
    }
}

namespace Pos.Application.Features.Reports;

public sealed record SalesReportQuery
{
    public ReportPeriod Period { get; init; } = ReportPeriod.Daily;

    /// <summary>Any store-local date inside the wanted period; defaults to today.</summary>
    public DateOnly? Date { get; init; }
}

public sealed record SalesReportDto(
    ReportPeriod Period,
    DateOnly From,
    DateOnly To,
    SalesReportSummary Summary,
    IReadOnlyList<SalesReportBucket> Buckets,
    IReadOnlyList<TopProductDto> TopProducts,
    IReadOnlyList<CashierSalesDto> Cashiers);

public sealed record SalesReportSummary(
    int SaleCount,
    int ReturnCount,
    decimal ItemsSold,
    decimal GrossSales,
    decimal ItemDiscounts,
    decimal BillDiscounts,
    decimal NetSales,
    decimal Returns,
    decimal NetRevenue,
    decimal CostOfGoods,
    decimal GrossProfit,
    decimal Expenses,
    decimal NetProfit,
    decimal CashSales,
    decimal CardSales,
    decimal AverageSale);

public sealed record SalesReportBucket(string Label, DateTime Start, int SaleCount, decimal NetSales, decimal Returns, decimal NetRevenue);

public sealed record TopProductDto(int ProductId, string ProductName, decimal Quantity, decimal Revenue);

public sealed record CashierSalesDto(int UserId, string UserName, int SaleCount, decimal Total);

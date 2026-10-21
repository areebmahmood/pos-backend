namespace Pos.Application.Features.Reports;

public sealed record SalesReportQuery
{
    public ReportPeriod Period { get; init; } = ReportPeriod.Daily;

    /// <summary>Any store-local date inside the wanted period; defaults to today.</summary>
    public DateOnly? Date { get; init; }
}

/// <param name="OwnSalesOnly">
/// True for salesmen: the report covers only the caller's sales and returns, and cost, profit, expenses
/// and cashier totals are left out (zero or empty).
/// </param>
public sealed record SalesReportDto(
    bool OwnSalesOnly,
    ReportPeriod Period,
    DateOnly From,
    DateOnly To,
    SalesReportSummary Summary,
    IReadOnlyList<SalesReportBucket> Buckets,
    IReadOnlyList<TopProductDto> TopProducts,
    IReadOnlyList<CashierSalesDto> Cashiers,
    IReadOnlyList<ExpenseCategoryDto> ExpenseCategories,
    IReadOnlyList<ReportExpenseDto> Expenses);

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
    decimal OnlineSales,
    decimal JazzCashSales,
    decimal EasyPaisaSales,
    decimal BankTransferSales,
    decimal AverageSale);

public sealed record SalesReportBucket(string Label, DateTime Start, int SaleCount, decimal NetSales, decimal Returns, decimal NetRevenue);

public sealed record TopProductDto(int ProductId, string ProductName, decimal Quantity, decimal Revenue);

/// <summary>Expenses grouped by category; expenses without one are grouped under <c>null</c>.</summary>
public sealed record ExpenseCategoryDto(string? Category, int Count, decimal Total);

public sealed record ReportExpenseDto(int Id, DateTime CreatedAt, string Title, string? Category, decimal Amount, string CreatedBy);

public sealed record CashierSalesDto(int UserId, string UserName, int SaleCount, decimal Total);

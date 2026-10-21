using Pos.Application.Common.Models;

namespace Pos.Application.Common.Abstractions.Persistence;

/// <summary>
/// Read-only aggregate queries executed in the database. Time buckets are computed in store-local time
/// by shifting UTC timestamps by <c>offsetMinutes</c>.
/// </summary>
public interface IReportQueries
{
    Task<IReadOnlyList<SalesHourAggregate>> GetSalesByHourAsync(
        DateRange range, int offsetMinutes, int? cashierId, CancellationToken ct);

    Task<IReadOnlyList<ReturnsHourAggregate>> GetReturnsByHourAsync(
        DateRange range, int offsetMinutes, int? processedById, CancellationToken ct);

    Task<decimal> GetExpensesTotalAsync(DateRange range, int? createdById, CancellationToken ct);

    /// <summary>Every expense in the range, oldest first.</summary>
    Task<IReadOnlyList<ExpenseRow>> GetExpensesAsync(DateRange range, CancellationToken ct);

    Task<ItemTotals> GetItemTotalsAsync(DateRange range, int? cashierId, CancellationToken ct);

    Task<IReadOnlyList<TopProductRow>> GetTopProductsAsync(DateRange range, int take, int? cashierId, CancellationToken ct);

    Task<IReadOnlyList<CashierRow>> GetCashierTotalsAsync(DateRange range, CancellationToken ct);

    /// <summary>Headline totals for a range, optionally limited to one user's sales, returns and expenses.</summary>
    Task<PeriodTotals> GetPeriodTotalsAsync(DateRange range, int? userId, CancellationToken ct);
}

public sealed record PeriodTotals(decimal NetSales, int SaleCount, decimal Returns, decimal Expenses);

public sealed record SalesHourAggregate(
    DateTime LocalHour,
    int SaleCount,
    decimal Subtotal,
    decimal ItemDiscount,
    decimal BillDiscount,
    decimal Total,
    decimal CashTotal,
    decimal CardTotal,
    decimal JazzCashTotal,
    decimal EasyPaisaTotal,
    decimal BankTransferTotal)
{
    public decimal OnlineTotal => JazzCashTotal + EasyPaisaTotal + BankTransferTotal;
}

public sealed record ReturnsHourAggregate(DateTime LocalHour, int ReturnCount, decimal RefundAmount, decimal CostAmount);

public sealed record ItemTotals(decimal ItemsSold, decimal CostOfGoods);

public sealed record TopProductRow(int ProductId, string ProductName, decimal Quantity, decimal Revenue);

public sealed record ExpenseRow(int Id, DateTime CreatedAt, string Title, string? Category, decimal Amount, string CreatedBy);

public sealed record CashierRow(int UserId, string UserName, int SaleCount, decimal Total);

using Microsoft.EntityFrameworkCore;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Models;
using Pos.Domain.Enums;

namespace Pos.Infrastructure.Persistence.Queries;

/// <summary>
/// Aggregations are pushed down to the database and grouped per local hour, which keeps the result set small
/// (at most 24 rows per day) no matter how many sales were made.
/// </summary>
internal sealed class ReportQueries(PosDbContext db) : IReportQueries
{
    public async Task<IReadOnlyList<SalesHourAggregate>> GetSalesByHourAsync(
        DateRange range, int offsetMinutes, int? cashierId, CancellationToken ct)
    {
        var rows = await db.Sales.AsNoTracking()
            .Where(s => s.CreatedAt >= range.FromUtc && s.CreatedAt < range.ToUtc)
            .Where(s => cashierId == null || s.CashierId == cashierId)
            .Select(s => new
            {
                Local = s.CreatedAt.AddMinutes(offsetMinutes),
                s.Subtotal,
                s.ItemDiscount,
                s.BillDiscount,
                s.Total,
                s.PaymentMethod,
                s.OnlinePaymentType
            })
            .GroupBy(s => new { s.Local.Year, s.Local.Month, s.Local.Day, s.Local.Hour })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Day,
                g.Key.Hour,
                Count = g.Count(),
                Subtotal = g.Sum(s => s.Subtotal),
                ItemDiscount = g.Sum(s => s.ItemDiscount),
                BillDiscount = g.Sum(s => s.BillDiscount),
                Total = g.Sum(s => s.Total),
                Cash = g.Sum(s => s.PaymentMethod == PaymentMethod.Cash ? s.Total : 0),
                Card = g.Sum(s => s.PaymentMethod == PaymentMethod.Card ? s.Total : 0),
                JazzCash = g.Sum(s => s.PaymentMethod == PaymentMethod.Online && s.OnlinePaymentType == OnlinePaymentType.JazzCash ? s.Total : 0),
                EasyPaisa = g.Sum(s => s.PaymentMethod == PaymentMethod.Online && s.OnlinePaymentType == OnlinePaymentType.EasyPaisa ? s.Total : 0),
                BankTransfer = g.Sum(s => s.PaymentMethod == PaymentMethod.Online && s.OnlinePaymentType == OnlinePaymentType.BankTransfer ? s.Total : 0)
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new SalesHourAggregate(
                new DateTime(r.Year, r.Month, r.Day, r.Hour, 0, 0),
                r.Count, r.Subtotal, r.ItemDiscount, r.BillDiscount, r.Total, r.Cash, r.Card, r.JazzCash, r.EasyPaisa, r.BankTransfer))
            .OrderBy(r => r.LocalHour)
            .ToList();
    }

    public async Task<IReadOnlyList<ReturnsHourAggregate>> GetReturnsByHourAsync(
        DateRange range, int offsetMinutes, int? processedById, CancellationToken ct)
    {
        var rows = await db.SaleReturns.AsNoTracking()
            .Where(r => r.CreatedAt >= range.FromUtc && r.CreatedAt < range.ToUtc)
            .Where(r => processedById == null || r.ProcessedById == processedById)
            .Select(r => new { Local = r.CreatedAt.AddMinutes(offsetMinutes), r.RefundAmount, r.CostAmount })
            .GroupBy(r => new { r.Local.Year, r.Local.Month, r.Local.Day, r.Local.Hour })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Day,
                g.Key.Hour,
                Count = g.Count(),
                Refund = g.Sum(r => r.RefundAmount),
                Cost = g.Sum(r => r.CostAmount)
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new ReturnsHourAggregate(new DateTime(r.Year, r.Month, r.Day, r.Hour, 0, 0), r.Count, r.Refund, r.Cost))
            .OrderBy(r => r.LocalHour)
            .ToList();
    }

    public async Task<decimal> GetExpensesTotalAsync(DateRange range, int? createdById, CancellationToken ct) =>
        await db.Expenses.AsNoTracking()
            .Where(e => e.CreatedAt >= range.FromUtc && e.CreatedAt < range.ToUtc)
            .Where(e => createdById == null || e.CreatedById == createdById)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0;

    public async Task<IReadOnlyList<ExpenseRow>> GetExpensesAsync(DateRange range, CancellationToken ct)
    {
        var rows = await db.Expenses.AsNoTracking()
            .Where(e => e.CreatedAt >= range.FromUtc && e.CreatedAt < range.ToUtc)
            .OrderBy(e => e.CreatedAt)
            .ThenBy(e => e.Id)
            .Select(e => new { e.Id, e.CreatedAt, e.Title, e.Category, e.Amount, CreatedBy = e.CreatedBy.FullName })
            .ToListAsync(ct);

        return rows.Select(r => new ExpenseRow(r.Id, r.CreatedAt, r.Title, r.Category, r.Amount, r.CreatedBy)).ToList();
    }

    public async Task<ItemTotals> GetItemTotalsAsync(DateRange range, CancellationToken ct)
    {
        var totals = await SaleItemsIn(range)
            .GroupBy(_ => 1)
            .Select(g => new { Quantity = g.Sum(i => i.Quantity), Cost = g.Sum(i => i.UnitCost * i.Quantity) })
            .FirstOrDefaultAsync(ct);

        return new ItemTotals(totals?.Quantity ?? 0, totals?.Cost ?? 0);
    }

    public async Task<IReadOnlyList<TopProductRow>> GetTopProductsAsync(DateRange range, int take, CancellationToken ct)
    {
        var rows = await SaleItemsIn(range)
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new { g.Key.ProductId, g.Key.ProductName, Quantity = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.LineTotal) })
            .OrderByDescending(p => p.Revenue)
            .ThenBy(p => p.ProductName)
            .Take(take)
            .ToListAsync(ct);

        return rows.Select(r => new TopProductRow(r.ProductId, r.ProductName, r.Quantity, r.Revenue)).ToList();
    }

    public async Task<IReadOnlyList<CashierRow>> GetCashierTotalsAsync(DateRange range, CancellationToken ct)
    {
        var rows = await db.Sales.AsNoTracking()
            .Where(s => s.CreatedAt >= range.FromUtc && s.CreatedAt < range.ToUtc)
            .GroupBy(s => new { s.CashierId, s.Cashier.FullName })
            .Select(g => new { g.Key.CashierId, g.Key.FullName, Count = g.Count(), Total = g.Sum(s => s.Total) })
            .OrderByDescending(c => c.Total)
            .ToListAsync(ct);

        return rows.Select(r => new CashierRow(r.CashierId, r.FullName, r.Count, r.Total)).ToList();
    }

    public async Task<PeriodTotals> GetPeriodTotalsAsync(DateRange range, int? userId, CancellationToken ct)
    {
        var sales = await db.Sales.AsNoTracking()
            .Where(s => s.CreatedAt >= range.FromUtc && s.CreatedAt < range.ToUtc)
            .Where(s => userId == null || s.CashierId == userId)
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Total = g.Sum(s => s.Total) })
            .FirstOrDefaultAsync(ct);

        var returns = await db.SaleReturns.AsNoTracking()
            .Where(r => r.CreatedAt >= range.FromUtc && r.CreatedAt < range.ToUtc)
            .Where(r => userId == null || r.ProcessedById == userId)
            .SumAsync(r => (decimal?)r.RefundAmount, ct) ?? 0;

        var expenses = await GetExpensesTotalAsync(range, userId, ct);
        return new PeriodTotals(sales?.Total ?? 0, sales?.Count ?? 0, returns, expenses);
    }

    private IQueryable<Domain.Entities.SaleItem> SaleItemsIn(DateRange range) =>
        db.Sales.AsNoTracking()
            .Where(s => s.CreatedAt >= range.FromUtc && s.CreatedAt < range.ToUtc)
            .SelectMany(s => s.Items);
}

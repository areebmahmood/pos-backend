using Pos.Application.Features.Products;
using Pos.Application.Features.Registers;
using Pos.Application.Features.Sales;

namespace Pos.Application.Features.Dashboard;

public sealed record PeriodSummaryDto(decimal NetSales, int SaleCount, decimal Returns, decimal Expenses);

public sealed record DailySalesDto(DateOnly Date, decimal NetSales, int SaleCount);

public sealed record AdminDashboardDto(
    PeriodSummaryDto Today,
    PeriodSummaryDto Month,
    int TotalProducts,
    int LowStockCount,
    IReadOnlyList<ProductDto> LowStockProducts,
    IReadOnlyList<RegisterSessionDto> OpenRegisters,
    IReadOnlyList<SaleSummaryDto> RecentSales,
    IReadOnlyList<DailySalesDto> Last7Days);

public sealed record SalesmanDashboardDto(
    RegisterSessionDto? Register,
    PeriodSummaryDto Today,
    IReadOnlyList<SaleSummaryDto> RecentSales);

namespace Pos.Application.Features.Dashboard;

public interface IDashboardService
{
    Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken ct);

    Task<SalesmanDashboardDto> GetSalesmanDashboardAsync(CancellationToken ct);
}

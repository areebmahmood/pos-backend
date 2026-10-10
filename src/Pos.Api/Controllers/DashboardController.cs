using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Features.Dashboard;
using Pos.Domain.Common;

namespace Pos.Api.Controllers;

[Route("api/dashboard")]
public sealed class DashboardController(IDashboardService dashboard) : ApiControllerBase
{
    [HttpGet("admin")]
    [Authorize(Roles = Roles.Admin)]
    public Task<AdminDashboardDto> Admin(CancellationToken ct) => dashboard.GetAdminDashboardAsync(ct);

    [HttpGet("salesman")]
    public Task<SalesmanDashboardDto> Salesman(CancellationToken ct) => dashboard.GetSalesmanDashboardAsync(ct);
}

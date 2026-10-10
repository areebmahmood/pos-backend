using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Features.Reports;
using Pos.Domain.Common;

namespace Pos.Api.Controllers;

[Route("api/reports")]
[Authorize(Roles = Roles.Admin)]
public sealed class ReportsController(IReportService reports) : ApiControllerBase
{
    /// <summary>Sales report for the day, week, month or year containing <c>date</c>.</summary>
    [HttpGet("sales")]
    public Task<SalesReportDto> Sales([FromQuery] SalesReportQuery query, CancellationToken ct) =>
        reports.GetSalesReportAsync(query, ct);
}

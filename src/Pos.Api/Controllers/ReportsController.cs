using Microsoft.AspNetCore.Mvc;
using Pos.Application.Features.Reports;

namespace Pos.Api.Controllers;

[Route("api/reports")]
public sealed class ReportsController(IReportService reports) : ApiControllerBase
{
    /// <summary>
    /// Sales report for the day, week, month or year containing <c>date</c>. Salesmen get their own sales only.
    /// </summary>
    [HttpGet("sales")]
    public Task<SalesReportDto> Sales([FromQuery] SalesReportQuery query, CancellationToken ct) =>
        reports.GetSalesReportAsync(query, ct);
}

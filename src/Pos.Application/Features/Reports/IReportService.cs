namespace Pos.Application.Features.Reports;

public interface IReportService
{
    Task<SalesReportDto> GetSalesReportAsync(SalesReportQuery query, CancellationToken ct);
}

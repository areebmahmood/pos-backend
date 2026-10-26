using Pos.Application.Common.Models;

namespace Pos.Application.Features.Sales;

public interface ISaleService
{
    Task<SaleDto> CreateAsync(CreateSaleRequest request, CancellationToken ct);

    Task<PagedResult<SaleSummaryDto>> ListAsync(SaleQuery query, CancellationToken ct);

    Task<SaleDto> GetAsync(int id, CancellationToken ct);

    Task<SaleDto> GetByInvoiceAsync(string invoiceNumber, CancellationToken ct);

    Task<ReceiptDto> GetReceiptAsync(int id, CancellationToken ct);

    /// <summary>Admin only: deletes a sale and its returns, putting unreturned items back in stock.</summary>
    Task DeleteAsync(int id, CancellationToken ct);
}

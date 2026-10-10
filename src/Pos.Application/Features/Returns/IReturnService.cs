using Pos.Application.Common.Models;

namespace Pos.Application.Features.Returns;

public interface IReturnService
{
    Task<SaleReturnDto> CreateAsync(CreateReturnRequest request, CancellationToken ct);

    Task<SaleReturnDto> GetAsync(int id, CancellationToken ct);

    Task<PagedResult<SaleReturnDto>> ListAsync(ReturnQuery query, CancellationToken ct);
}

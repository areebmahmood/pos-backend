using Pos.Application.Common.Models;

namespace Pos.Application.Features.DiscountCodes;

public interface IDiscountCodeService
{
    Task<PagedResult<DiscountCodeDto>> ListAsync(DiscountCodeQuery query, CancellationToken ct);

    Task<DiscountCodeDto> GetAsync(int id, CancellationToken ct);

    Task<DiscountCodeDto> CreateAsync(SaveDiscountCodeRequest request, CancellationToken ct);

    Task<DiscountCodeDto> UpdateAsync(int id, SaveDiscountCodeRequest request, CancellationToken ct);

    Task DeleteAsync(int id, CancellationToken ct);

    /// <summary>Checks a code at billing; throws with the reason when it cannot be used.</summary>
    Task<DiscountCodeCheckDto> CheckAsync(string code, decimal amount, CancellationToken ct);
}

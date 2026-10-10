using Pos.Application.Common.Models;

namespace Pos.Application.Features.Registers;

public interface IRegisterService
{
    /// <summary>The caller's open register, or <c>null</c> when none is open.</summary>
    Task<RegisterSessionDto?> GetCurrentAsync(CancellationToken ct);

    Task<RegisterSessionDto> GetAsync(int id, CancellationToken ct);

    Task<PagedResult<RegisterSessionDto>> ListAsync(RegisterQuery query, CancellationToken ct);

    Task<IReadOnlyList<RegisterSessionDto>> ListOpenAsync(CancellationToken ct);

    Task<RegisterSessionDto> OpenAsync(OpenRegisterRequest request, CancellationToken ct);

    Task<RegisterSessionDto> CloseAsync(int id, CloseRegisterRequest request, CancellationToken ct);
}

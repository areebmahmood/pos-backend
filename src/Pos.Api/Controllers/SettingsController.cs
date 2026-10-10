using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Features.Settings;
using Pos.Domain.Common;

namespace Pos.Api.Controllers;

[Route("api/settings")]
public sealed class SettingsController(ISettingsService settings) : ApiControllerBase
{
    [HttpGet]
    public Task<StoreSettingsDto> Get(CancellationToken ct) => settings.GetAsync(ct);

    [HttpPut]
    [Authorize(Roles = Roles.Admin)]
    public Task<StoreSettingsDto> Update(UpdateStoreSettingsRequest request, CancellationToken ct) =>
        settings.UpdateAsync(request, ct);
}

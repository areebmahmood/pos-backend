using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Features.Settings;
using Pos.Domain.Common;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;

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

    [HttpGet("whatsapp-group-qr")]
    public async Task<IActionResult> GetWhatsAppGroupQr(CancellationToken ct)
    {
        var image = await settings.GetWhatsAppGroupQrAsync(ct);
        return File(image.Content, image.ContentType);
    }

    [HttpPut("whatsapp-group-qr")]
    [Authorize(Roles = Roles.Admin)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ProductImage.MaxBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = ProductImage.MaxBytes + 64 * 1024)]
    public async Task<StoreSettingsDto> SetWhatsAppGroupQr(IFormFile file, CancellationToken ct)
    {
        if (file.Length > ProductImage.MaxBytes)
            throw new DomainException($"The image must be at most {ProductImage.MaxBytes / 1024} KB.");

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, ct);
        return await settings.SetWhatsAppGroupQrAsync(buffer.ToArray(), ct);
    }

    [HttpDelete("whatsapp-group-qr")]
    [Authorize(Roles = Roles.Admin)]
    public Task<StoreSettingsDto> RemoveWhatsAppGroupQr(CancellationToken ct) => settings.RemoveWhatsAppGroupQrAsync(ct);

    [HttpGet("whatsapp")]
    [Authorize(Roles = Roles.Admin)]
    public Task<WhatsAppSettingsDto> GetWhatsApp(CancellationToken ct) => settings.GetWhatsAppAsync(ct);

    [HttpPut("whatsapp")]
    [Authorize(Roles = Roles.Admin)]
    public Task<WhatsAppSettingsDto> UpdateWhatsApp(UpdateWhatsAppSettingsRequest request, CancellationToken ct) =>
        settings.UpdateWhatsAppAsync(request, ct);

    [HttpPost("whatsapp/test")]
    [Authorize(Roles = Roles.Admin)]
    public Task<WhatsAppTestResult> TestWhatsApp(WhatsAppTestRequest request, CancellationToken ct) =>
        settings.SendWhatsAppTestAsync(request, ct);
}

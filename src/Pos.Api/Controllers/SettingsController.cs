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

    // Uploaded store images: store-logo, receipt-logo and whatsapp-group-qr (see StoreImage.Kinds).
    private const string ImageRoute = "{kind:regex(^(store-logo|receipt-logo|whatsapp-group-qr)$)}";

    [HttpGet(ImageRoute)]
    public async Task<IActionResult> GetImage(string kind, CancellationToken ct)
    {
        var image = await settings.GetImageAsync(kind, ct);
        return File(image.Content, image.ContentType);
    }

    [HttpPut(ImageRoute)]
    [Authorize(Roles = Roles.Admin)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ProductImage.MaxBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = ProductImage.MaxBytes + 64 * 1024)]
    public async Task<StoreSettingsDto> SetImage(string kind, IFormFile file, CancellationToken ct)
    {
        if (file.Length > ProductImage.MaxBytes)
            throw new DomainException($"The image must be at most {ProductImage.MaxBytes / 1024} KB.");

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, ct);
        return await settings.SetImageAsync(kind, buffer.ToArray(), ct);
    }

    [HttpDelete(ImageRoute)]
    [Authorize(Roles = Roles.Admin)]
    public Task<StoreSettingsDto> RemoveImage(string kind, CancellationToken ct) => settings.RemoveImageAsync(kind, ct);

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

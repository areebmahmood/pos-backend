using FluentValidation;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Abstractions.Services;
using Pos.Application.Common.Exceptions;
using Pos.Application.Common.Validation;
using Pos.Application.Features.Messaging;
using Pos.Domain.Common;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;

namespace Pos.Application.Features.Settings;

public sealed class SettingsService(
    IStoreSettingsRepository settings,
    IUnitOfWork unitOfWork,
    IStoreClock storeClock,
    IDateTimeProvider clock,
    IWhatsAppClient whatsApp,
    IValidator<UpdateStoreSettingsRequest> validator,
    IValidator<UpdateWhatsAppSettingsRequest> whatsAppValidator) : ISettingsService
{
    public async Task<StoreSettingsDto> GetAsync(CancellationToken ct) => await ToDtoAsync(await settings.GetAsync(ct), ct);

    public async Task<StoreSettingsDto> UpdateAsync(UpdateStoreSettingsRequest request, CancellationToken ct)
    {
        await validator.EnsureValidAsync(request, ct);
        var entity = await settings.GetAsync(ct);
        entity.Update(
            request.StoreName, request.Address, request.Phone, request.ReceiptFooter,
            request.UrduInstructions, request.WhatsAppGroupCaption, request.CurrencySymbol);

        await unitOfWork.SaveChangesAsync(ct);
        return await ToDtoAsync(entity, ct);
    }

    public async Task<StoreImageFile> GetWhatsAppGroupQrAsync(CancellationToken ct)
    {
        var image = await settings.GetImageAsync(StoreImage.WhatsAppGroupQr, ct)
            ?? throw new NotFoundException("No WhatsApp group QR code has been uploaded.");
        return new StoreImageFile(image.Content, image.ContentType);
    }

    public async Task<StoreSettingsDto> SetWhatsAppGroupQrAsync(byte[] content, CancellationToken ct)
    {
        var image = await settings.GetImageAsync(StoreImage.WhatsAppGroupQr, ct);
        if (image is null)
            settings.AddImage(new StoreImage(StoreImage.WhatsAppGroupQr, content, clock.UtcNow));
        else
            image.Replace(content, clock.UtcNow);

        await unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(ct);
    }

    public async Task<StoreSettingsDto> RemoveWhatsAppGroupQrAsync(CancellationToken ct)
    {
        if (await settings.GetImageAsync(StoreImage.WhatsAppGroupQr, ct) is { } image)
        {
            settings.RemoveImage(image);
            await unitOfWork.SaveChangesAsync(ct);
        }

        return await GetAsync(ct);
    }

    public async Task<WhatsAppSettingsDto> GetWhatsAppAsync(CancellationToken ct) =>
        (await settings.GetAsync(ct)).ToWhatsAppDto();

    public async Task<WhatsAppSettingsDto> UpdateWhatsAppAsync(UpdateWhatsAppSettingsRequest request, CancellationToken ct)
    {
        await whatsAppValidator.EnsureValidAsync(request, ct);
        var entity = await settings.GetAsync(ct);
        entity.UpdateWhatsApp(
            request.Enabled, request.PhoneNumberId, request.AccessToken, request.ThankYouTemplate,
            request.PromotionTemplate, request.TemplateLanguage, request.DefaultCountryCode);

        await unitOfWork.SaveChangesAsync(ct);
        return entity.ToWhatsAppDto();
    }

    public async Task<WhatsAppTestResult> SendWhatsAppTestAsync(WhatsAppTestRequest request, CancellationToken ct)
    {
        var entity = await settings.GetAsync(ct);
        if (entity.WhatsAppPhoneNumberId is null || entity.WhatsAppAccessToken is null)
            throw new DomainException("Save the phone number ID and access token first.");

        var phone = PhoneNumber.Normalize(request.Phone, entity.DefaultCountryCode)
            ?? throw new DomainException("Enter a valid phone number to send the test to.");

        // Prefer the store's own templates so the test proves they are approved and have the right variables.
        var (template, language, parameters) = entity switch
        {
            { WhatsAppThankYouTemplate: { } name } => (name, entity.WhatsAppTemplateLanguage,
                WhatsAppTemplates.ThankYou("Test customer", "INV-TEST-000001", 1000, entity.CurrencySymbol)),
            { WhatsAppPromotionTemplate: { } name } => (name, entity.WhatsAppTemplateLanguage,
                WhatsAppTemplates.Promotion("Test customer", "This is a test message.")),
            _ => (WhatsAppTemplates.HelloWorld, "en_US", Array.Empty<string>())
        };

        var account = new WhatsAppAccount(entity.WhatsAppPhoneNumberId, entity.WhatsAppAccessToken);
        var result = await whatsApp.SendTemplateAsync(account, phone, template, language, parameters, ct);
        return new WhatsAppTestResult(result.Success, template, result.Error);
    }

    private async Task<StoreSettingsDto> ToDtoAsync(StoreSettings entity, CancellationToken ct) =>
        entity.ToDto(storeClock.TimeZoneId, await settings.GetImageUpdatedAtAsync(StoreImage.WhatsAppGroupQr, ct));
}

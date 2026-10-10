using FluentValidation;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Abstractions.Services;
using Pos.Application.Common.Validation;

namespace Pos.Application.Features.Settings;

public sealed class SettingsService(
    IStoreSettingsRepository settings,
    IUnitOfWork unitOfWork,
    IStoreClock storeClock,
    IValidator<UpdateStoreSettingsRequest> validator) : ISettingsService
{
    public async Task<StoreSettingsDto> GetAsync(CancellationToken ct) =>
        (await settings.GetAsync(ct)).ToDto(storeClock.TimeZoneId);

    public async Task<StoreSettingsDto> UpdateAsync(UpdateStoreSettingsRequest request, CancellationToken ct)
    {
        await validator.EnsureValidAsync(request, ct);
        var entity = await settings.GetAsync(ct);
        entity.Update(
            request.StoreName, request.Address, request.Phone, request.ReceiptFooter,
            request.UrduInstructions, request.ReceiptQrBaseUrl, request.CurrencySymbol);

        await unitOfWork.SaveChangesAsync(ct);
        return entity.ToDto(storeClock.TimeZoneId);
    }
}

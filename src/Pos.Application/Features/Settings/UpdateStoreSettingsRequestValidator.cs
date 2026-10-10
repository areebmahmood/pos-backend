using FluentValidation;

namespace Pos.Application.Features.Settings;

public sealed class UpdateStoreSettingsRequestValidator : AbstractValidator<UpdateStoreSettingsRequest>
{
    public UpdateStoreSettingsRequestValidator()
    {
        RuleFor(x => x.StoreName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Address).MaximumLength(300);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.ReceiptFooter).MaximumLength(500);
        RuleFor(x => x.UrduInstructions).MaximumLength(2000);
        RuleFor(x => x.ReceiptQrBaseUrl).MaximumLength(300)
            .Must(url => string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("QR base URL must be an absolute URL.");
        RuleFor(x => x.CurrencySymbol).NotEmpty().MaximumLength(10);
    }
}

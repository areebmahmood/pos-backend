using FluentValidation;
using Pos.Domain.Entities;

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
        RuleFor(x => x.WhatsAppGroupCaption).MaximumLength(200);
        RuleFor(x => x.CurrencySymbol).NotEmpty().MaximumLength(10);
    }
}

public sealed class UpdateWhatsAppSettingsRequestValidator : AbstractValidator<UpdateWhatsAppSettingsRequest>
{
    public UpdateWhatsAppSettingsRequestValidator()
    {
        RuleFor(x => x.PhoneNumberId).MaximumLength(50).Matches("^[0-9]*$").WithMessage("The phone number ID is digits only.");
        RuleFor(x => x.AccessToken).MaximumLength(1000);
        RuleFor(x => x.ThankYouTemplate).MaximumLength(OutgoingMessage.TemplateMaxLength);
        RuleFor(x => x.PromotionTemplate).MaximumLength(OutgoingMessage.TemplateMaxLength);
        RuleFor(x => x.TemplateLanguage).NotEmpty().MaximumLength(OutgoingMessage.LanguageMaxLength);
        RuleFor(x => x.DefaultCountryCode).NotEmpty().Matches(@"^\+?[0-9]{1,4}$").WithMessage("Enter digits only, e.g. 92.");
    }
}

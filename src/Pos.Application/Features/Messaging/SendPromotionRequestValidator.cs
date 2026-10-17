using FluentValidation;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Messaging;

public sealed class SendPromotionRequestValidator : AbstractValidator<SendPromotionRequest>
{
    public SendPromotionRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(Promotion.MessageMaxLength);
    }
}

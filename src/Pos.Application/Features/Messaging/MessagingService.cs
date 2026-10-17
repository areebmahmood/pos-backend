using FluentValidation;
using Microsoft.Extensions.Logging;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Abstractions.Services;
using Pos.Application.Common.Exceptions;
using Pos.Application.Common.Models;
using Pos.Application.Common.Validation;
using Pos.Domain.Entities;
using Pos.Domain.Enums;
using Pos.Domain.Exceptions;

namespace Pos.Application.Features.Messaging;

public sealed class MessagingService(
    IMessageRepository messages,
    ICustomerRepository customers,
    IStoreSettingsRepository settings,
    IUnitOfWork unitOfWork,
    IWhatsAppClient whatsApp,
    IMessageDispatchSignal dispatchSignal,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    IValidator<SendPromotionRequest> validator,
    ILogger<MessagingService> logger) : IMessagingService
{
    private const int DispatchBatchSize = 20;

    public async Task<PromotionDto> SendPromotionAsync(SendPromotionRequest request, CancellationToken ct)
    {
        await validator.EnsureValidAsync(request, ct);
        var store = await settings.GetAsync(ct);
        if (!store.WhatsAppConfigured)
            throw new DomainException("Turn on WhatsApp messaging in Settings before sending promotions.");
        if (store.WhatsAppPromotionTemplate is null)
            throw new DomainException("Enter the promotion template name in Settings before sending promotions.");

        var ids = request.CustomerIds is { Count: > 0 } ? request.CustomerIds.Distinct().ToList() : null;
        var recipients = await customers.ListPromotionRecipientsAsync(ids, ct);
        if (recipients.Count == 0)
            throw new DomainException("There are no customers to message (customers who opted out are skipped).");

        var promotion = new Promotion(request.Message, currentUser.UserId, recipients.Count);
        messages.AddPromotion(promotion);
        await unitOfWork.SaveChangesAsync(ct);

        var now = clock.UtcNow;
        foreach (var customer in recipients)
        {
            var parameters = WhatsAppTemplates.Promotion(customer.Name, promotion.Message);
            messages.Add(new OutgoingMessage(
                MessageKind.Promotion, customer.Phone, store.WhatsAppPromotionTemplate, store.WhatsAppTemplateLanguage,
                WhatsAppTemplates.Serialize(parameters), now, customer.Id, promotionId: promotion.Id));
        }

        await unitOfWork.SaveChangesAsync(ct);
        dispatchSignal.Notify();
        return await messages.FindPromotionAsync(promotion.Id, MessagingMappings.PromotionProjection, ct)
            ?? throw NotFoundException.For("Promotion", promotion.Id);
    }

    public Task<PagedResult<PromotionDto>> ListPromotionsAsync(PageRequest page, CancellationToken ct) =>
        messages.ListPromotionsAsync(page, MessagingMappings.PromotionProjection, ct);

    public Task<PagedResult<OutgoingMessageDto>> ListMessagesAsync(MessageQuery query, CancellationToken ct) =>
        messages.ListAsync(query, MessagingMappings.Projection, ct);

    public async Task<int> DispatchPendingAsync(CancellationToken ct)
    {
        var pending = await messages.GetPendingAsync(DispatchBatchSize, clock.UtcNow, ct);
        if (pending.Count == 0)
            return 0;

        var store = await settings.GetAsync(ct);
        foreach (var message in pending)
        {
            if (!store.WhatsAppConfigured)
            {
                message.MarkFailed("WhatsApp messaging is turned off in Settings.", retryable: false, clock.UtcNow);
                continue;
            }

            var account = new WhatsAppAccount(store.WhatsAppPhoneNumberId!, store.WhatsAppAccessToken!);
            var result = await whatsApp.SendTemplateAsync(
                account, message.Phone, message.TemplateName, message.Language,
                WhatsAppTemplates.Deserialize(message.Parameters), ct);

            if (result.Success)
            {
                message.MarkSent(clock.UtcNow);
            }
            else
            {
                logger.LogWarning("WhatsApp message {MessageId} to {Phone} failed: {Error}", message.Id, message.Phone, result.Error);
                message.MarkFailed(result.Error ?? "Unknown error.", result.Retryable, clock.UtcNow);
            }

            // Save as we go, so a crash never sends the same message twice.
            await unitOfWork.SaveChangesAsync(ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return pending.Count;
    }
}

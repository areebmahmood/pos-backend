using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Persistence.Configurations;

internal sealed class StoreSettingsConfiguration : IEntityTypeConfiguration<StoreSettings>
{
    public void Configure(EntityTypeBuilder<StoreSettings> builder)
    {
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.StoreName).HasMaxLength(150).IsRequired();
        builder.Property(s => s.Address).HasMaxLength(300);
        builder.Property(s => s.Phone).HasMaxLength(50);
        builder.Property(s => s.ReceiptFooter).HasMaxLength(500);
        builder.Property(s => s.UrduInstructions).HasMaxLength(2000);
        builder.Property(s => s.WhatsAppGroupCaption).HasMaxLength(200);
        builder.Property(s => s.CurrencySymbol).HasMaxLength(10).IsRequired();
        builder.Property(s => s.DefaultCountryCode).HasMaxLength(5).IsRequired().HasDefaultValue("92");
        builder.Property(s => s.WhatsAppPhoneNumberId).HasMaxLength(50);
        builder.Property(s => s.WhatsAppAccessToken).HasMaxLength(1000);
        builder.Property(s => s.WhatsAppThankYouTemplate).HasMaxLength(OutgoingMessage.TemplateMaxLength);
        builder.Property(s => s.WhatsAppPromotionTemplate).HasMaxLength(OutgoingMessage.TemplateMaxLength);
        builder.Property(s => s.WhatsAppTemplateLanguage).HasMaxLength(OutgoingMessage.LanguageMaxLength).IsRequired().HasDefaultValue("en");
        builder.Ignore(s => s.WhatsAppConfigured);
    }
}

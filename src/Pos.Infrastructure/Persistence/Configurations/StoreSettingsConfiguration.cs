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
        builder.Property(s => s.ReceiptQrBaseUrl).HasMaxLength(300);
        builder.Property(s => s.CurrencySymbol).HasMaxLength(10).IsRequired();
    }
}

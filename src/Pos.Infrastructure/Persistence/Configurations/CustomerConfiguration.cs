using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.Domain.Common;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(Customer.NameMaxLength);
        builder.Property(c => c.Phone).HasMaxLength(PhoneNumber.MaxLength).IsRequired();
        builder.HasIndex(c => c.Phone).IsUnique();
        builder.HasIndex(c => c.Name);

        builder.HasMany(c => c.Sales).WithOne(s => s.Customer).HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.SetNull);
        builder.Navigation(c => c.Sales).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCode>
{
    public void Configure(EntityTypeBuilder<DiscountCode> builder)
    {
        builder.Property(d => d.Code).HasMaxLength(DiscountCode.CodeMaxLength).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(DiscountCode.DescriptionMaxLength);
        builder.HasIndex(d => d.Code).IsUnique();
    }
}

internal sealed class StoreImageConfiguration : IEntityTypeConfiguration<StoreImage>
{
    public void Configure(EntityTypeBuilder<StoreImage> builder)
    {
        builder.HasKey(i => i.Kind);
        builder.Property(i => i.Kind).HasMaxLength(StoreImage.KindMaxLength);
        builder.Property(i => i.Content).HasMaxLength(ProductImage.MaxBytes).IsRequired();
        builder.Property(i => i.ContentType).HasMaxLength(ProductImage.ContentTypeMaxLength).IsRequired();
    }
}

internal sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.Property(p => p.Message).HasMaxLength(Promotion.MessageMaxLength).IsRequired();
        builder.HasOne(p => p.CreatedBy).WithMany().HasForeignKey(p => p.CreatedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(p => p.Messages).WithOne().HasForeignKey(m => m.PromotionId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Messages).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(p => p.CreatedAt);
    }
}

internal sealed class OutgoingMessageConfiguration : IEntityTypeConfiguration<OutgoingMessage>
{
    public void Configure(EntityTypeBuilder<OutgoingMessage> builder)
    {
        builder.Property(m => m.Phone).HasMaxLength(PhoneNumber.MaxLength).IsRequired();
        builder.Property(m => m.TemplateName).HasMaxLength(OutgoingMessage.TemplateMaxLength).IsRequired();
        builder.Property(m => m.Language).HasMaxLength(OutgoingMessage.LanguageMaxLength).IsRequired();
        builder.Property(m => m.Parameters).HasMaxLength(OutgoingMessage.ParametersMaxLength).IsRequired();
        builder.Property(m => m.Error).HasMaxLength(OutgoingMessage.ErrorMaxLength);

        builder.HasOne(m => m.Customer).WithMany().HasForeignKey(m => m.CustomerId).OnDelete(DeleteBehavior.SetNull);
        // Sales are never deleted; NoAction also avoids SQL Server's multiple-cascade-path error.
        builder.HasOne<Sale>().WithMany().HasForeignKey(m => m.SaleId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(m => new { m.Status, m.Id });
        builder.HasIndex(m => m.CreatedAt);
    }
}

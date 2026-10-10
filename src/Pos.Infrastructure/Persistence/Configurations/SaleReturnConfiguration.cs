using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Persistence.Configurations;

internal sealed class SaleReturnConfiguration : IEntityTypeConfiguration<SaleReturn>
{
    public void Configure(EntityTypeBuilder<SaleReturn> builder)
    {
        builder.Property(r => r.Reason).HasMaxLength(SaleReturn.ReasonMaxLength);

        builder.HasOne(r => r.RegisterSession).WithMany().HasForeignKey(r => r.RegisterSessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.ProcessedBy).WithMany().HasForeignKey(r => r.ProcessedById).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Items).WithOne().HasForeignKey(i => i.SaleReturnId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(r => r.CreatedAt);
        builder.HasIndex(r => r.RegisterSessionId);
    }
}

internal sealed class SaleReturnItemConfiguration : IEntityTypeConfiguration<SaleReturnItem>
{
    public void Configure(EntityTypeBuilder<SaleReturnItem> builder)
    {
        builder.Property(i => i.ProductName).HasMaxLength(Product.NameMaxLength).IsRequired();
        builder.Property(i => i.Quantity).IsQuantity();

        builder.HasOne(i => i.SaleItem).WithMany().HasForeignKey(i => i.SaleItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(i => i.ProductId);
    }
}

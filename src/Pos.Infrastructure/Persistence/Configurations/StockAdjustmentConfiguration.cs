using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Persistence.Configurations;

internal sealed class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.Property(a => a.QuantityChange).IsQuantity();
        builder.Property(a => a.QuantityBefore).IsQuantity();
        builder.Property(a => a.QuantityAfter).IsQuantity();
        builder.Property(a => a.Reason).HasMaxLength(StockAdjustment.ReasonMaxLength);

        builder.HasOne(a => a.Product).WithMany().HasForeignKey(a => a.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.ProductId, a.CreatedAt });
        builder.HasIndex(a => a.CreatedAt);
    }
}

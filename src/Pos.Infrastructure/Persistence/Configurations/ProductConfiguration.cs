using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(Product.NameMaxLength).IsRequired();
        builder.Property(p => p.Barcode).HasMaxLength(Product.BarcodeMaxLength).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(Product.DescriptionMaxLength);
        builder.Property(p => p.StockQuantity).IsQuantity();
        builder.Property(p => p.ReorderLevel).IsQuantity();
        builder.Property(p => p.ConcurrencyStamp).IsConcurrencyToken();

        builder.Ignore(p => p.EffectivePrice);
        builder.Ignore(p => p.IsLowStock);

        // Barcodes are unique among live products; a deleted product's barcode can be reused.
        builder.HasIndex(p => p.Barcode).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.CategoryId);

        builder.HasOne(p => p.Image)
            .WithOne()
            .HasForeignKey<ProductImage>(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}

internal sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.HasKey(i => i.ProductId);
        builder.Property(i => i.ProductId).ValueGeneratedNever();
        builder.Property(i => i.Content).HasMaxLength(ProductImage.MaxBytes).IsRequired();
        builder.Property(i => i.ContentType).HasMaxLength(ProductImage.ContentTypeMaxLength).IsRequired();
    }
}

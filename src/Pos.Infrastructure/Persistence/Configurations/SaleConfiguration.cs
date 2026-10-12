using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Persistence.Configurations;

internal sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.Property(s => s.InvoiceNumber).HasMaxLength(Sale.InvoiceNumberMaxLength).IsRequired();
        builder.Property(s => s.CustomerName).HasMaxLength(Sale.CustomerNameMaxLength);
        builder.Property(s => s.CustomerPhone).HasMaxLength(Sale.CustomerPhoneMaxLength);
        builder.Ignore(s => s.NetBeforeBillDiscount);

        builder.HasOne(s => s.RegisterSession).WithMany().HasForeignKey(s => s.RegisterSessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Cashier).WithMany().HasForeignKey(s => s.CashierId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Items).WithOne().HasForeignKey(i => i.SaleId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(s => s.Returns).WithOne(r => r.Sale).HasForeignKey(r => r.SaleId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(s => s.Returns).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(s => s.InvoiceNumber).IsUnique();
        builder.HasIndex(s => s.CreatedAt);
        builder.HasIndex(s => new { s.CashierId, s.CreatedAt });
        builder.HasIndex(s => s.RegisterSessionId);
    }
}

internal sealed class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.Property(i => i.ProductName).HasMaxLength(Product.NameMaxLength).IsRequired();
        builder.Property(i => i.Barcode).HasMaxLength(Product.BarcodeMaxLength).IsRequired();
        builder.Property(i => i.Quantity).IsQuantity();
        builder.Property(i => i.ReturnedQuantity).IsQuantity();
        builder.Property(i => i.UnitDiscount).HasPrecision(18, 4);
        builder.Ignore(i => i.ReturnableQuantity);

        builder.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(i => i.ProductId);
    }
}

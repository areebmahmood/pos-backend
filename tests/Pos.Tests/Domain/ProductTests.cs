using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Infrastructure.Services;

namespace Pos.Tests.Domain;

public sealed class ProductTests
{
    private static Product NewProduct() => new("Tea", "123", null, null, costPrice: 80, salePrice: 100, discountedPrice: null, reorderLevel: 2);

    [Fact]
    public void Discounted_price_must_be_below_sale_price()
    {
        var product = NewProduct();

        Assert.Throws<DomainException>(() => product.SetDiscount(100));
        product.SetDiscount(90);

        Assert.Equal(90, product.EffectivePrice);
    }

    [Fact]
    public void Stock_cannot_go_negative()
    {
        var product = NewProduct();
        product.AdjustStock(3);

        Assert.Throws<DomainException>(() => product.AdjustStock(-4));
        Assert.Equal(3, product.AdjustStock(-3));
        Assert.True(product.IsLowStock);
    }

    [Theory]
    [InlineData("400638133393", 1)]
    [InlineData("590123412345", 7)]
    public void Ean13_check_digit_matches_the_standard(string payload, int expected) =>
        Assert.Equal(expected, Ean13BarcodeGenerator.CheckDigit(payload));

    [Theory]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0 }, "image/png")]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, "image/jpeg")]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, "image/gif")]
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50 }, "image/webp")]
    [InlineData(new byte[] { 0x3C, 0x73, 0x76, 0x67 }, null)]
    public void Barcode_image_type_comes_from_the_file_signature(byte[] content, string? expected) =>
        Assert.Equal(expected, ProductImage.DetectContentType(content));

    [Fact]
    public void Barcode_image_rejects_oversized_or_unknown_files()
    {
        Assert.Throws<DomainException>(() => new ProductImage(1, new byte[ProductImage.MaxBytes + 1], DateTime.UtcNow));
        Assert.Throws<DomainException>(() => new ProductImage(1, "plain text"u8.ToArray(), DateTime.UtcNow));
    }
}

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
}

using System.Linq.Expressions;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Products;

public static class ProductMappings
{
    public static readonly Expression<Func<Product, ProductDto>> Projection = p => new ProductDto(
        p.Id,
        p.Name,
        p.Barcode,
        p.Description,
        p.CategoryId,
        p.Category != null ? p.Category.Name : null,
        p.CostPrice,
        p.SalePrice,
        p.DiscountedPrice,
        p.DiscountedPrice ?? p.SalePrice,
        p.DiscountedPrice == null || p.SalePrice == 0
            ? 0
            : Math.Round((p.SalePrice - p.DiscountedPrice.Value) * 100 / p.SalePrice, 2),
        p.StockQuantity,
        p.ReorderLevel,
        p.StockQuantity <= p.ReorderLevel,
        p.IsActive,
        p.Image != null,
        p.Image != null ? p.Image.UpdatedAt : null,
        p.CreatedAt,
        p.UpdatedAt);
}

using Pos.Application.Common.Models;

namespace Pos.Application.Features.Products;

public sealed record ProductDto(
    int Id,
    string Name,
    string Barcode,
    string? Description,
    int? CategoryId,
    string? CategoryName,
    decimal CostPrice,
    decimal SalePrice,
    decimal? DiscountedPrice,
    decimal EffectivePrice,
    decimal DiscountPercent,
    decimal StockQuantity,
    decimal ReorderLevel,
    bool IsLowStock,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CreateProductRequest(
    string Name,
    string? Barcode,
    string? Description,
    int? CategoryId,
    decimal CostPrice,
    decimal SalePrice,
    decimal? DiscountedPrice,
    decimal StockQuantity,
    decimal ReorderLevel);

public sealed record UpdateProductRequest(
    string Name,
    string? Barcode,
    string? Description,
    int? CategoryId,
    decimal CostPrice,
    decimal SalePrice,
    decimal? DiscountedPrice,
    decimal ReorderLevel,
    bool IsActive);

public sealed record SetDiscountRequest(decimal? DiscountedPrice);

public sealed record ProductQuery : PageRequest
{
    public string? Search { get; init; }
    public int? CategoryId { get; init; }
    public bool OnlyDiscounted { get; init; }
    public bool LowStock { get; init; }
    public bool IncludeInactive { get; init; } = true;
}

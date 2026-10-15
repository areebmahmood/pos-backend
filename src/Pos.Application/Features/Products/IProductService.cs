using Pos.Application.Common.Models;

namespace Pos.Application.Features.Products;

public interface IProductService
{
    Task<PagedResult<ProductDto>> ListAsync(ProductQuery query, CancellationToken ct);

    Task<ProductDto> GetAsync(int id, CancellationToken ct);

    Task<ProductDto> GetByBarcodeAsync(string barcode, CancellationToken ct);

    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct);

    Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct);

    Task<ProductDto> SetDiscountAsync(int id, SetDiscountRequest request, CancellationToken ct);

    Task DeleteAsync(int id, CancellationToken ct);

    Task<ProductImageFile> GetBarcodeImageAsync(int id, CancellationToken ct);

    Task<ProductDto> SetBarcodeImageAsync(int id, byte[] content, CancellationToken ct);

    Task<ProductDto> RemoveBarcodeImageAsync(int id, CancellationToken ct);
}

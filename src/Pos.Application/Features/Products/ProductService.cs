using FluentValidation;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Abstractions.Services;
using Pos.Application.Common.Exceptions;
using ValidationException = Pos.Application.Common.Exceptions.ValidationException;
using Pos.Application.Common.Models;
using Pos.Application.Common.Validation;
using Pos.Domain.Entities;
using Pos.Domain.Enums;

namespace Pos.Application.Features.Products;

public sealed class ProductService(
    IProductRepository products,
    ICategoryRepository categories,
    IStockAdjustmentRepository stockAdjustments,
    IUnitOfWork unitOfWork,
    IBarcodeGenerator barcodeGenerator,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    IValidator<CreateProductRequest> createValidator,
    IValidator<UpdateProductRequest> updateValidator,
    IValidator<SetDiscountRequest> discountValidator) : IProductService
{
    public Task<PagedResult<ProductDto>> ListAsync(ProductQuery query, CancellationToken ct) =>
        products.ListAsync(query, ProductMappings.Projection, ct);

    public async Task<ProductDto> GetAsync(int id, CancellationToken ct) =>
        await products.FindAsync(id, ProductMappings.Projection, ct) ?? throw NotFoundException.For("Product", id);

    public async Task<ProductDto> GetByBarcodeAsync(string barcode, CancellationToken ct) =>
        await products.FindByBarcodeAsync(barcode.Trim(), ProductMappings.Projection, ct)
        ?? throw new NotFoundException($"No product found with barcode '{barcode}'.");

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct)
    {
        await createValidator.EnsureValidAsync(request, ct);
        await EnsureCategoryExistsAsync(request.CategoryId, ct);
        var barcode = await ResolveBarcodeAsync(request.Barcode, null, ct);

        var product = new Product(
            request.Name, barcode, request.Description, request.CategoryId,
            request.CostPrice, request.SalePrice, request.DiscountedPrice, request.ReorderLevel);
        products.Add(product);

        if (request.StockQuantity > 0)
        {
            var before = product.AdjustStock(request.StockQuantity);
            stockAdjustments.Add(new StockAdjustment(
                product, StockAdjustmentType.Initial, request.StockQuantity, before, currentUser.UserId, "Opening stock"));
        }

        await unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(product.Id, ct);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct)
    {
        await updateValidator.EnsureValidAsync(request, ct);
        var product = await GetEntityAsync(id, ct);
        await EnsureCategoryExistsAsync(request.CategoryId, ct);
        var barcode = await ResolveBarcodeAsync(request.Barcode, product, ct);

        product.UpdateDetails(
            request.Name, barcode, request.Description, request.CategoryId,
            request.CostPrice, request.SalePrice, request.DiscountedPrice, request.ReorderLevel, request.IsActive);

        await unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<ProductDto> SetDiscountAsync(int id, SetDiscountRequest request, CancellationToken ct)
    {
        await discountValidator.EnsureValidAsync(request, ct);
        var product = await GetEntityAsync(id, ct);

        product.SetDiscount(request.DiscountedPrice);
        await unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var product = await GetEntityAsync(id, ct);
        product.Delete(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<ProductImageFile> GetBarcodeImageAsync(int id, CancellationToken ct)
    {
        var image = await products.GetImageAsync(id, ct)
            ?? throw new NotFoundException($"Product {id} has no barcode image.");
        return new ProductImageFile(image.Content, image.ContentType);
    }

    public async Task<ProductDto> SetBarcodeImageAsync(int id, byte[] content, CancellationToken ct)
    {
        await GetEntityAsync(id, ct);
        var image = await products.GetImageAsync(id, ct);
        if (image is null)
            products.AddImage(new ProductImage(id, content, clock.UtcNow));
        else
            image.Replace(content, clock.UtcNow);

        await unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<ProductDto> RemoveBarcodeImageAsync(int id, CancellationToken ct)
    {
        await GetEntityAsync(id, ct);
        if (await products.GetImageAsync(id, ct) is { } image)
        {
            products.RemoveImage(image);
            await unitOfWork.SaveChangesAsync(ct);
        }
        return await GetAsync(id, ct);
    }

    private async Task<Product> GetEntityAsync(int id, CancellationToken ct) =>
        await products.GetByIdAsync(id, ct) ?? throw NotFoundException.For("Product", id);

    private async Task EnsureCategoryExistsAsync(int? categoryId, CancellationToken ct)
    {
        if (categoryId is { } value && !await categories.ExistsAsync(value, ct))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["categoryId"] = [$"Category {value} does not exist."]
            });
    }

    /// <summary>Keeps the existing barcode when blank on update, generates one when blank on create, and enforces uniqueness.</summary>
    private async Task<string> ResolveBarcodeAsync(string? requested, Product? existing, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requested))
            return existing?.Barcode ?? await barcodeGenerator.GenerateUniqueAsync(ct);

        var barcode = requested.Trim();
        if (barcode != existing?.Barcode && await products.BarcodeExistsAsync(barcode, existing?.Id, ct))
            throw new ConflictException($"Barcode '{barcode}' is already assigned to another product.");

        return barcode;
    }
}

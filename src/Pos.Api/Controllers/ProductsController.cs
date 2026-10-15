using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Common.Models;
using Pos.Application.Features.Products;
using Pos.Domain.Common;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;

namespace Pos.Api.Controllers;

[Route("api/products")]
public sealed class ProductsController(IProductService products) : ApiControllerBase
{
    [HttpGet]
    public Task<PagedResult<ProductDto>> List([FromQuery] ProductQuery query, CancellationToken ct) =>
        products.ListAsync(query, ct);

    [HttpGet("{id:int}")]
    public Task<ProductDto> Get(int id, CancellationToken ct) => products.GetAsync(id, ct);

    [HttpGet("barcode/{barcode}")]
    public Task<ProductDto> GetByBarcode(string barcode, CancellationToken ct) => products.GetByBarcodeAsync(barcode, ct);

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateProductRequest request, CancellationToken ct)
    {
        var product = await products.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public Task<ProductDto> Update(int id, UpdateProductRequest request, CancellationToken ct) =>
        products.UpdateAsync(id, request, ct);

    [HttpPut("{id:int}/discount")]
    [Authorize(Roles = Roles.Admin)]
    public Task<ProductDto> SetDiscount(int id, SetDiscountRequest request, CancellationToken ct) =>
        products.SetDiscountAsync(id, request, ct);

    [HttpGet("{id:int}/barcode-image")]
    public async Task<IActionResult> GetBarcodeImage(int id, CancellationToken ct)
    {
        var image = await products.GetBarcodeImageAsync(id, ct);
        return File(image.Content, image.ContentType);
    }

    [HttpPut("{id:int}/barcode-image")]
    [Authorize(Roles = Roles.Admin)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ProductImage.MaxBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = ProductImage.MaxBytes + 64 * 1024)]
    public async Task<ProductDto> SetBarcodeImage(int id, IFormFile file, CancellationToken ct)
    {
        if (file.Length > ProductImage.MaxBytes)
            throw new DomainException($"The image must be at most {ProductImage.MaxBytes / 1024} KB.");

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, ct);
        return await products.SetBarcodeImageAsync(id, buffer.ToArray(), ct);
    }

    [HttpDelete("{id:int}/barcode-image")]
    [Authorize(Roles = Roles.Admin)]
    public Task<ProductDto> RemoveBarcodeImage(int id, CancellationToken ct) =>
        products.RemoveBarcodeImageAsync(id, ct);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await products.DeleteAsync(id, ct);
        return NoContent();
    }
}

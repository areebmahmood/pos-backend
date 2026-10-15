using System.Linq.Expressions;
using Pos.Application.Common.Models;
using Pos.Application.Features.Products;
using Pos.Domain.Entities;

namespace Pos.Application.Common.Abstractions.Persistence;

public interface IProductRepository : IRepository<Product>
{
    Task<TResult?> FindAsync<TResult>(int id, Expression<Func<Product, TResult>> selector, CancellationToken ct);

    Task<TResult?> FindByBarcodeAsync<TResult>(string barcode, Expression<Func<Product, TResult>> selector, CancellationToken ct);

    /// <summary>Checks barcode uniqueness among live (not deleted) products.</summary>
    Task<bool> BarcodeExistsAsync(string barcode, int? excludeId, CancellationToken ct);

    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct);

    Task<PagedResult<TResult>> ListAsync<TResult>(
        ProductQuery query, Expression<Func<Product, TResult>> selector, CancellationToken ct);

    Task<int> CountAsync(bool lowStockOnly, CancellationToken ct);

    /// <summary>The barcode-label image of a live product, tracked for update.</summary>
    Task<ProductImage?> GetImageAsync(int productId, CancellationToken ct);

    void AddImage(ProductImage image);

    void RemoveImage(ProductImage image);
}

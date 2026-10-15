using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Models;
using Pos.Application.Features.Products;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(PosDbContext db) : Repository<Product>(db), IProductRepository
{
    public Task<TResult?> FindAsync<TResult>(int id, Expression<Func<Product, TResult>> selector, CancellationToken ct) =>
        Set.AsNoTracking().Where(p => p.Id == id).Select(selector).FirstOrDefaultAsync(ct);

    public Task<TResult?> FindByBarcodeAsync<TResult>(string barcode, Expression<Func<Product, TResult>> selector, CancellationToken ct) =>
        Set.AsNoTracking().Where(p => p.Barcode == barcode).Select(selector).FirstOrDefaultAsync(ct);

    public Task<bool> BarcodeExistsAsync(string barcode, int? excludeId, CancellationToken ct) =>
        Set.AnyAsync(p => p.Barcode == barcode && (excludeId == null || p.Id != excludeId), ct);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        await Set.Where(p => ids.Contains(p.Id)).ToListAsync(ct);

    public Task<PagedResult<TResult>> ListAsync<TResult>(
        ProductQuery query, Expression<Func<Product, TResult>> selector, CancellationToken ct)
    {
        var term = query.Search.AsSearchTerm();
        return Set.AsNoTracking()
            .WhereIf(term is not null, p =>
                p.Name.Contains(term!) || p.Barcode == term || (p.Description != null && p.Description.Contains(term!)))
            .WhereIf(query.CategoryId is not null, p => p.CategoryId == query.CategoryId)
            .WhereIf(query.OnlyDiscounted, p => p.DiscountedPrice != null)
            .WhereIf(query.LowStock, p => p.StockQuantity <= p.ReorderLevel)
            .WhereIf(!query.IncludeInactive, p => p.IsActive)
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToPagedResultAsync(query, selector, ct);
    }

    public Task<int> CountAsync(bool lowStockOnly, CancellationToken ct) =>
        Set.WhereIf(lowStockOnly, p => p.IsActive && p.StockQuantity <= p.ReorderLevel).CountAsync(ct);

    public Task<ProductImage?> GetImageAsync(int productId, CancellationToken ct) =>
        Set.Where(p => p.Id == productId).Select(p => p.Image).FirstOrDefaultAsync(ct);

    public void AddImage(ProductImage image) => Db.ProductImages.Add(image);

    public void RemoveImage(ProductImage image) => Db.ProductImages.Remove(image);
}

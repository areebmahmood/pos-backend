using FluentValidation;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Exceptions;
using Pos.Application.Common.Validation;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Categories;

public sealed class CategoryService(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    IValidator<SaveCategoryRequest> validator) : ICategoryService
{
    public Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken ct) =>
        categories.ListAsync(CategoryMappings.Projection, ct);

    public async Task<CategoryDto> GetAsync(int id, CancellationToken ct) =>
        await categories.FindAsync(id, CategoryMappings.Projection, ct) ?? throw NotFoundException.For("Category", id);

    public async Task<CategoryDto> CreateAsync(SaveCategoryRequest request, CancellationToken ct)
    {
        await validator.EnsureValidAsync(request, ct);
        await EnsureUniqueNameAsync(request.Name, null, ct);

        var category = new Category(request.Name, request.Description);
        categories.Add(category);
        await unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(category.Id, ct);
    }

    public async Task<CategoryDto> UpdateAsync(int id, SaveCategoryRequest request, CancellationToken ct)
    {
        await validator.EnsureValidAsync(request, ct);
        var category = await categories.GetByIdAsync(id, ct) ?? throw NotFoundException.For("Category", id);
        await EnsureUniqueNameAsync(request.Name, id, ct);

        category.Update(request.Name, request.Description);
        await unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var category = await categories.GetByIdAsync(id, ct) ?? throw NotFoundException.For("Category", id);
        if (await categories.HasProductsAsync(id, ct))
            throw new ConflictException("Move or delete the products in this category before deleting it.");

        categories.Remove(category);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task EnsureUniqueNameAsync(string name, int? excludeId, CancellationToken ct)
    {
        if (await categories.NameExistsAsync(name.Trim(), excludeId, ct))
            throw new ConflictException($"A category named '{name.Trim()}' already exists.");
    }
}

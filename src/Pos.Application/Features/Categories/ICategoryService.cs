namespace Pos.Application.Features.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken ct);

    Task<CategoryDto> GetAsync(int id, CancellationToken ct);

    Task<CategoryDto> CreateAsync(SaveCategoryRequest request, CancellationToken ct);

    Task<CategoryDto> UpdateAsync(int id, SaveCategoryRequest request, CancellationToken ct);

    Task DeleteAsync(int id, CancellationToken ct);
}

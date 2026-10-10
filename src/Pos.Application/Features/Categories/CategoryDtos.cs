namespace Pos.Application.Features.Categories;

public sealed record CategoryDto(int Id, string Name, string? Description, int ProductCount);

public sealed record SaveCategoryRequest(string Name, string? Description);

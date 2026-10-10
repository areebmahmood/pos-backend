using System.Linq.Expressions;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Categories;

public static class CategoryMappings
{
    public static readonly Expression<Func<Category, CategoryDto>> Projection =
        c => new CategoryDto(c.Id, c.Name, c.Description, c.Products.Count());
}

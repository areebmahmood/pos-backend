using Pos.Domain.Common;

namespace Pos.Domain.Entities;

public class Category : AuditableEntity
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;

    private readonly List<Product> _products = [];

    private Category() { }

    public Category(string name, string? description)
    {
        Update(name, description);
    }

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public IReadOnlyCollection<Product> Products => _products;

    public void Update(string name, string? description)
    {
        Name = Guard.NotEmpty(name, "Category name", NameMaxLength);
        Description = Guard.Optional(description, "Description", DescriptionMaxLength);
    }
}

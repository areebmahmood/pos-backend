namespace Pos.Infrastructure.Persistence.Seeding;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public string AdminUsername { get; init; } = "admin";

    /// <summary>Initial admin password; used only when the database has no users. Change it after first login.</summary>
    public string AdminPassword { get; init; } = "Admin@123";

    /// <summary>Adds a demo salesman, categories and products to an empty database.</summary>
    public bool DemoData { get; init; }
}

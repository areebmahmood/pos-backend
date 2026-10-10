using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pos.Application.Common.Abstractions.Services;
using Pos.Domain.Entities;
using Pos.Domain.Enums;

namespace Pos.Infrastructure.Persistence.Seeding;

public sealed class DatabaseInitializer(
    PosDbContext db,
    IPasswordHasher passwordHasher,
    IOptions<SeedOptions> seedOptions,
    ILogger<DatabaseInitializer> logger)
{
    private readonly SeedOptions _seed = seedOptions.Value;

    public async Task InitializeAsync(bool applyMigrations, CancellationToken ct = default)
    {
        if (applyMigrations && db.Database.IsRelational())
        {
            logger.LogInformation("Applying database migrations");
            await db.Database.MigrateAsync(ct);
        }

        await SeedAsync(ct);
    }

    private async Task SeedAsync(CancellationToken ct)
    {
        if (!await db.StoreSettings.AnyAsync(ct))
            db.StoreSettings.Add(StoreSettings.CreateDefault());

        if (!await db.Users.AnyAsync(ct))
        {
            logger.LogWarning("No users found; creating initial admin '{Username}'. Change its password after first login.", _seed.AdminUsername);
            db.Users.Add(new User(_seed.AdminUsername, "Store Admin", UserRole.Admin, passwordHasher.Hash(_seed.AdminPassword)));

            if (_seed.DemoData)
                db.Users.Add(new User("salesman", "Demo Salesman", UserRole.Salesman, passwordHasher.Hash("Sales@123")));
        }

        await db.SaveChangesAsync(ct);

        if (_seed.DemoData && !await db.Products.IgnoreQueryFilters().AnyAsync(ct))
            await SeedDemoCatalogAsync(ct);
    }

    private async Task SeedDemoCatalogAsync(CancellationToken ct)
    {
        logger.LogInformation("Seeding demo catalog");
        var admin = await db.Users.FirstAsync(u => u.Role == UserRole.Admin, ct);

        var grocery = new Category("Grocery", "Rice, flour, pulses and daily essentials");
        var beverages = new Category("Beverages", "Soft drinks, juices and tea");
        var personalCare = new Category("Personal Care", "Soaps, shampoos and toiletries");
        db.Categories.AddRange(grocery, beverages, personalCare);
        await db.SaveChangesAsync(ct);

        (string Name, string Barcode, Category Category, decimal Cost, decimal Price, decimal? Discounted, decimal Stock, string? Description)[] items =
        [
            ("Basmati Rice 1kg", "2000000000015", grocery, 280, 350, 320, 60, "Premium long-grain basmati rice"),
            ("Chakki Atta 5kg", "2000000000022", grocery, 700, 820, null, 40, "Stone-ground whole wheat flour"),
            ("Daal Masoor 1kg", "2000000000039", grocery, 300, 360, null, 8, null),
            ("Cooking Oil 1L", "2000000000046", grocery, 520, 590, 560, 35, "Canola cooking oil"),
            ("Cola 1.5L", "2000000000053", beverages, 140, 180, null, 120, null),
            ("Mango Juice 1L", "2000000000060", beverages, 190, 250, 230, 50, "100% mango pulp"),
            ("Green Tea 25 bags", "2000000000077", beverages, 210, 280, null, 5, null),
            ("Bath Soap 120g", "2000000000084", personalCare, 90, 120, 110, 200, null),
            ("Herbal Shampoo 400ml", "2000000000091", personalCare, 480, 620, null, 25, "For all hair types"),
            ("Toothpaste 150g", "2000000000107", personalCare, 220, 290, null, 70, null)
        ];

        foreach (var item in items)
        {
            var product = new Product(item.Name, item.Barcode, item.Description, item.Category.Id,
                item.Cost, item.Price, item.Discounted, reorderLevel: 10);
            var before = product.AdjustStock(item.Stock);
            db.Products.Add(product);
            db.StockAdjustments.Add(new StockAdjustment(product, StockAdjustmentType.Initial, item.Stock, before, admin.Id, "Opening stock"));
        }

        await db.SaveChangesAsync(ct);
    }
}

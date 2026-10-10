using Pos.Infrastructure.Persistence.Seeding;

namespace Pos.Api.Extensions;

internal static class WebApplicationExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        var applyMigrations = app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true);
        await initializer.InitializeAsync(applyMigrations);
    }
}

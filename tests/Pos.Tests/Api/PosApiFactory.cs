using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pos.Infrastructure.Persistence;
using Pos.Infrastructure.Persistence.Interceptors;
using Pos.Tests.Infrastructure;

namespace Pos.Tests.Api;

/// <summary>Hosts the real API pipeline against an in-memory SQLite database.</summary>
public sealed class PosApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public PosApiFactory()
    {
        _connection.Open();
        using var db = new SqlitePosDbContext(new DbContextOptionsBuilder<PosDbContext>().UseSqlite(_connection).Options);
        db.Database.EnsureCreated();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");
        builder.UseSetting("Seed:DemoData", "true");

        builder.ConfigureServices(services =>
        {
            var efRegistrations = services
                .Where(d => d.ServiceType == typeof(PosDbContext) ||
                            (d.ServiceType.IsGenericType && d.ServiceType.GenericTypeArguments.Contains(typeof(PosDbContext))))
                .ToList();
            foreach (var descriptor in efRegistrations)
                services.Remove(descriptor);

            services.AddScoped<PosDbContext>(sp => new SqlitePosDbContext(
                new DbContextOptionsBuilder<PosDbContext>()
                    .UseSqlite(_connection)
                    .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>())
                    .Options));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}

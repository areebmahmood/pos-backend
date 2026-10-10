using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Pos.Infrastructure.Persistence;

/// <summary>Lets <c>dotnet ef</c> create the context without starting the API.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PosDbContext>
{
    public PosDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("POS_MIGRATIONS_CONNECTION")
            ?? "Server=localhost;Database=PosDb;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new PosDbContext(options);
    }
}

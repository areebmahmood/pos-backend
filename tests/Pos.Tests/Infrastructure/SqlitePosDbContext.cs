using Microsoft.EntityFrameworkCore;
using Pos.Infrastructure.Persistence;

namespace Pos.Tests.Infrastructure;

/// <summary>
/// SQLite cannot aggregate or order decimals natively, so tests store them as REAL.
/// Production uses SQL Server with exact decimal columns.
/// </summary>
public sealed class SqlitePosDbContext(DbContextOptions<PosDbContext> options) : PosDbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<decimal>().HaveConversion<double>();
        configurationBuilder.Properties<decimal?>().HaveConversion<double?>();
    }
}

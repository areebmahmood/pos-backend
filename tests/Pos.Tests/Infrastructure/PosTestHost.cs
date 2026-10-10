using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pos.Application;
using Pos.Application.Common.Abstractions.Services;
using Pos.Domain.Entities;
using Pos.Domain.Enums;
using Pos.Infrastructure;
using Pos.Infrastructure.Persistence;
using Pos.Infrastructure.Persistence.Interceptors;
using Pos.Infrastructure.Security;
using Pos.Infrastructure.Services;

namespace Pos.Tests.Infrastructure;

/// <summary>
/// Wires the real application and infrastructure services against an in-memory SQLite database.
/// </summary>
public sealed class PosTestHost : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public PosTestHost()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{JwtOptions.SectionName}:Issuer"] = "test",
            [$"{JwtOptions.SectionName}:Audience"] = "test",
            [$"{JwtOptions.SectionName}:SigningKey"] = "test-signing-key-0123456789-0123456789",
            [$"{StoreOptions.SectionName}:TimeZoneId"] = "Asia/Karachi"
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName));
        services.AddOptions<StoreOptions>().Bind(configuration.GetSection(StoreOptions.SectionName));

        services.AddApplication();
        services.AddCoreServices();
        services.AddRepositories();

        services.AddSingleton(CurrentUser);
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddSingleton(Clock);
        services.AddSingleton<IDateTimeProvider>(Clock);
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddScoped<PosDbContext>(sp => new SqlitePosDbContext(
            new DbContextOptionsBuilder<PosDbContext>()
                .UseSqlite(_connection)
                .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>())
                .Options));

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<PosDbContext>().Database.EnsureCreated();
    }

    public TestCurrentUser CurrentUser { get; } = new();

    public TestClock Clock { get; } = new();

    /// <summary>Resolves a service in a fresh scope, like a new HTTP request.</summary>
    public async Task<TResult> RunAsync<TService, TResult>(Func<TService, Task<TResult>> action) where TService : notnull
    {
        await using var scope = _provider.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    public async Task RunAsync<TService>(Func<TService, Task> action) where TService : notnull
    {
        await using var scope = _provider.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    public Task<TResult> QueryDbAsync<TResult>(Func<PosDbContext, Task<TResult>> query) => RunAsync(query);

    public async Task<User> AddUserAsync(string username, UserRole role)
    {
        return await RunAsync<PosDbContext, User>(async db =>
        {
            var user = new User(username, username, role, "hash");
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user;
        });
    }

    public void SignInAs(User user)
    {
        CurrentUser.Id = user.Id;
        CurrentUser.CurrentRole = user.Role;
    }

    public async ValueTask DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

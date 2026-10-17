using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Abstractions.Services;
using Pos.Infrastructure.Messaging;
using Pos.Infrastructure.Persistence;
using Pos.Infrastructure.Persistence.Interceptors;
using Pos.Infrastructure.Persistence.Queries;
using Pos.Infrastructure.Persistence.Repositories;
using Pos.Infrastructure.Persistence.Seeding;
using Pos.Infrastructure.Security;
using Pos.Infrastructure.Services;

namespace Pos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<StoreOptions>().Bind(configuration.GetSection(StoreOptions.SectionName));
        services.AddOptions<SeedOptions>().Bind(configuration.GetSection(SeedOptions.SectionName));

        services.AddOptions<WhatsAppOptions>().Bind(configuration.GetSection(WhatsAppOptions.SectionName));

        services.AddPersistence(configuration);
        services.AddCoreServices();
        services.AddWhatsApp();
        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddScoped<AuditableEntityInterceptor>();
        services.AddDbContext<PosDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(PosDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure(maxRetryCount: 3);
            });
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddRepositories();
    }

    /// <summary>Registers repositories and the unit of work; shared with tests that use another provider.</summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockAdjustmentRepository, StockAdjustmentRepository>();
        services.AddScoped<IRegisterSessionRepository, RegisterSessionRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<ISaleReturnRepository, SaleReturnRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IStoreSettingsRepository, StoreSettingsRepository>();
        services.AddScoped<IReportQueries, ReportQueries>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IDiscountCodeRepository, DiscountCodeRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<DatabaseInitializer>();
        return services;
    }

    /// <summary>Registers infrastructure services that do not depend on the database provider.</summary>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IStoreClock, StoreClock>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IInvoiceNumberGenerator, InvoiceNumberGenerator>();
        services.AddScoped<IBarcodeGenerator, Ean13BarcodeGenerator>();
        services.AddSingleton<MessageDispatchSignal>();
        services.AddSingleton<IMessageDispatchSignal>(sp => sp.GetRequiredService<MessageDispatchSignal>());
        return services;
    }

    /// <summary>The WhatsApp Cloud API client and the background worker that sends queued messages.</summary>
    private static void AddWhatsApp(this IServiceCollection services)
    {
        services.AddHttpClient<IWhatsAppClient, WhatsAppCloudClient>((sp, http) =>
        {
            var options = sp.GetRequiredService<IOptions<WhatsAppOptions>>().Value;
            http.BaseAddress = new Uri(options.GraphApiBaseUrl.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(20);
        });
        services.AddHostedService<MessageDispatcher>();
    }
}

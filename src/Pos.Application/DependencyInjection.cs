using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Pos.Application.Features.Auth;
using Pos.Application.Features.Categories;
using Pos.Application.Features.Customers;
using Pos.Application.Features.Dashboard;
using Pos.Application.Features.DiscountCodes;
using Pos.Application.Features.Expenses;
using Pos.Application.Features.Messaging;
using Pos.Application.Features.Products;
using Pos.Application.Features.Registers;
using Pos.Application.Features.Reports;
using Pos.Application.Features.Returns;
using Pos.Application.Features.Sales;
using Pos.Application.Features.Settings;
using Pos.Application.Features.Stock;
using Pos.Application.Features.Users;

namespace Pos.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Singleton);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<IRegisterService, RegisterService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IReturnService, ReturnService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IDiscountCodeService, DiscountCodeService>();
        services.AddScoped<IMessagingService, MessagingService>();

        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Budgets;
using Microsoft.Extensions.Configuration;
using Oficina.Application.Customers;
using Oficina.Application.Mechanics;
using Oficina.Application.Metrics;
using Oficina.Application.Notifications;
using Oficina.Application.OrderServiceHistory;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Application.WorkshopServices;
using Oficina.Application.Stocks;
using Oficina.Application.Vehicles;
using Oficina.Infrastructure.Persistence;
using Oficina.Infrastructure.Notifications;

namespace Oficina.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string? connectionString,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IPartRepository, PartRepository>();
        services.AddScoped<IStockRepository, StockPartRepository>();
        services.AddScoped<IWorkshopServiceRepository, WorkshopServiceRepository>();
        services.AddScoped<IServiceOrderRepository, ServiceOrderRepository>();
        services.AddScoped<IMechanicRepository, MechanicRepository>();
        services.AddScoped<IServiceOrderHistoryRepository, ServiceOrderHistoryRepository>();
        services.AddScoped<IWorkshopServiceExecutionTimeRepository, WorkshopServiceExecutionTimeRepository>();
        services.AddScoped<ServiceOrderHistoryService>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddScoped<INotificationEmailSender, SmtpNotificationEmailSender>();
        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Oficina.Application.Customers;
using Oficina.Application.Mechanics;
using Oficina.Application.OrderServiceHistory;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Application.Services;
using Oficina.Application.Stocks;
using Oficina.Application.Vehicles;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string? connectionString)
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
        services.AddScoped<ServiceOrderHistoryService>();
        return services;
    }
}

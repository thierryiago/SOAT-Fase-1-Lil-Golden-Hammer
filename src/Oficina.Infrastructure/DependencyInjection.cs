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
<<<<<<< HEAD
        services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
        services.AddSingleton<IVehicleRepository, InMemoryVehicleRepository>();
        services.AddScoped<IPartRepository, EfPartRepository>();
        services.AddSingleton<IWorkshopServiceRepository, InMemoryWorkshopServiceRepository>();
        services.AddSingleton<IServiceOrderRepository, InMemoryServiceOrderRepository>();
        services.AddScoped<IStockRepository, EfStockPartRepository>();
=======
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IPartRepository, PartRepository>();
        services.AddScoped<IWorkshopServiceRepository, WorkshopServiceRepository>();
        services.AddScoped<IServiceOrderRepository, ServiceOrderRepository>();
        services.AddScoped<IMechanicRepository, MechanicRepository>();
        services.AddScoped<IServiceOrderHistoryRepository, ServiceOrderHistoryRepository>();
        services.AddScoped<ServiceOrderHistoryService>();
>>>>>>> 96bf54b9426b5c6140207fe6e47b6e88fc4d58d2
        return services;
    }
}

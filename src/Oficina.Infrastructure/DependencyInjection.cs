using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Customers;
using Oficina.Application.Mechanics;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Application.Services;
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
        services.AddScoped<ICustomerRepository, InMemoryCustomerRepository>();
        services.AddScoped<IVehicleRepository, InMemoryVehicleRepository>();
        services.AddScoped<IPartRepository, InMemoryPartRepository>();
        services.AddScoped<IWorkshopServiceRepository, InMemoryWorkshopServiceRepository>();
        services.AddScoped<IServiceOrderRepository, InMemoryServiceOrderRepository>();
        services.AddScoped<IMechanicRepository, MechanicRepository>();
        return services;
    }
}

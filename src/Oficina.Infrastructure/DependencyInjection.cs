using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Customers;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Application.Services;
using Oficina.Application.Vehicles;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
        services.AddSingleton<IVehicleRepository, InMemoryVehicleRepository>();
        services.AddSingleton<IPartRepository, InMemoryPartRepository>();
        services.AddSingleton<IWorkshopServiceRepository, InMemoryWorkshopServiceRepository>();
        services.AddSingleton<IServiceOrderRepository, InMemoryServiceOrderRepository>();
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Customers;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
        services.AddSingleton<IPartRepository, InMemoryPartRepository>();
        services.AddSingleton<IServiceOrderRepository, InMemoryServiceOrderRepository>();
        return services;
    }
}

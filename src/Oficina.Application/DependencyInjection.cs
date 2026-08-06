using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Customers;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Application.Servicos;
using Oficina.Application.Stocks;
using Oficina.Application.Vehicles;

namespace Oficina.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CustomerService>();
        services.AddScoped<PartService>();
        services.AddScoped<ServiceOrderService>();
        services.AddScoped<VehicleService>();
        services.AddScoped<ServiceCatalogService>();
        services.AddScoped<StockService>();
        return services;
    }
}

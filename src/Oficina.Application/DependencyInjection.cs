using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Budgets;
using Oficina.Application.Customers;
using Oficina.Application.Mechanics;
using Oficina.Application.Metrics;
using Oficina.Application.Notifications;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Application.ServiceOrders.UseCases;
using Oficina.Application.ServiceOrders.UseCases.Queries;
using Oficina.Application.Stocks;
using Oficina.Application.Vehicles;
using Oficina.Application.WorkshopServices;

namespace Oficina.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CustomerService>();
        services.AddScoped<PartService>();
        services.AddScoped<ServiceOrderService>();
        services.AddScoped<OpenServiceOrderUseCase>();
        services.AddScoped<UpdateServiceOrderUseCase>();
        services.AddScoped<ListServiceOrdersUseCase>();
        services.AddScoped<GetServiceOrderByIdUseCase>();
        services.AddScoped<TrackServiceOrderUseCase>();
        services.AddScoped<TrackServiceOrdersByDocumentUseCase>();
        services.AddScoped<ListSchedulesUseCase>();
        services.AddScoped<ListSchedulesByDateUseCase>();
        services.AddScoped<VehicleService>();
        services.AddScoped<ServiceCatalogService>();
        services.AddScoped<StockService>();
        services.AddScoped<MechanicService>();
        services.AddScoped<MetricsService>();
        services.AddScoped<BudgetService>();
        services.AddScoped<IBudgetService>(provider => provider.GetRequiredService<BudgetService>());
        services.AddScoped<NotificationService>();
        return services;
    }
}

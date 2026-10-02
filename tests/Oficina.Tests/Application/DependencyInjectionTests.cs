using Microsoft.Extensions.DependencyInjection;
using Oficina.Application;
using Oficina.Application.Budgets;
using Oficina.Application.Customers;
using Oficina.Application.Mechanics.UseCases;
using Oficina.Application.Mechanics.UseCases.Queries;
using Oficina.Application.Metrics;
using Oficina.Application.Notifications;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Application.ServiceOrders.UseCases;
using Oficina.Application.ServiceOrders.UseCases.Queries;
using Oficina.Application.Stocks;
using Oficina.Application.Vehicles;
using Oficina.Application.WorkshopServices;

namespace Oficina.Tests.Application;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_should_register_all_services_as_scoped()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        Type[] expectedServices =
        [
            typeof(CustomerService),
            typeof(PartService),
            typeof(ServiceOrderService),
            typeof(OpenServiceOrderUseCase),
            typeof(UpdateServiceOrderUseCase),
            typeof(ListServiceOrdersUseCase),
            typeof(GetServiceOrderByIdUseCase),
            typeof(TrackServiceOrderUseCase),
            typeof(TrackServiceOrdersByDocumentUseCase),
            typeof(ListSchedulesUseCase),
            typeof(ListSchedulesByDateUseCase),
            typeof(VehicleService),
            typeof(ServiceCatalogService),
            typeof(StockService),
            typeof(CreateMechanicUseCase),
            typeof(UpdateMechanicUseCase),
            typeof(DeleteMechanicUseCase),
            typeof(ListMechanicsUseCase),
            typeof(GetMechanicByIdUseCase),
            typeof(MetricsService),
            typeof(BudgetService),
            typeof(NotificationService),
        ];

        foreach (var serviceType in expectedServices)
        {
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == serviceType && descriptor.Lifetime == ServiceLifetime.Scoped);
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Oficina.Application;
using Oficina.Application.Budgets;
using Oficina.Application.Customers.UseCases;
using Oficina.Application.Customers.UseCases.Queries;
using Oficina.Application.Mechanics;
using Oficina.Application.Metrics;
using Oficina.Application.Notifications;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Application.ServiceOrders.UseCases;
using Oficina.Application.ServiceOrders.UseCases.Queries;
using Oficina.Application.Stocks;
using Oficina.Application.Vehicles.UseCases;
using Oficina.Application.Vehicles.UseCases.Queries;
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
            typeof(CreateCustomerUseCase),
            typeof(UpdateCustomerUseCase),
            typeof(DeleteCustomerUseCase),
            typeof(ListCustomersUseCase),
            typeof(GetCustomerByIdUseCase),
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
            typeof(CreateVehicleUseCase),
            typeof(IdentifyCustomerAndRegisterVehicleUseCase),
            typeof(UpdateVehicleUseCase),
            typeof(DeleteVehicleUseCase),
            typeof(ListVehiclesUseCase),
            typeof(GetVehicleByIdUseCase),
            typeof(ServiceCatalogService),
            typeof(StockService),
            typeof(MechanicService),
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

using Microsoft.Extensions.DependencyInjection;
using Oficina.Application;
using Oficina.Application.Budgets;
using Oficina.Application.Customers.UseCases;
using Oficina.Application.Customers.UseCases.Queries;
using Oficina.Application.Mechanics.UseCases;
using Oficina.Application.Mechanics.UseCases.Queries;
using Oficina.Application.Metrics.UseCases.Queries;
using Oficina.Application.Parts;
using Oficina.Application.Parts.UseCases;
using Oficina.Application.Parts.UseCases.Queries;
using Oficina.Application.ServiceOrders;
using Oficina.Application.ServiceOrders.UseCases;
using Oficina.Application.ServiceOrders.UseCases.Queries;
using Oficina.Application.Stocks;
using Oficina.Application.Stocks.UseCases;
using Oficina.Application.Stocks.UseCases.Queries;
using Oficina.Application.Vehicles.UseCases;
using Oficina.Application.Vehicles.UseCases.Queries;
using Oficina.Application.WorkshopServices.UseCases;
using Oficina.Application.WorkshopServices.UseCases.Queries;

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
            typeof(CreatePartUseCase),
            typeof(UpdatePartUseCase),
            typeof(AdjustPartStockUseCase),
            typeof(DeletePartUseCase),
            typeof(ListPartsUseCase),
            typeof(GetPartByIdUseCase),
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
            typeof(CreateWorkshopServiceUseCase),
            typeof(UpdateWorkshopServiceUseCase),
            typeof(DeleteWorkshopServiceUseCase),
            typeof(ListWorkshopServicesUseCase),
            typeof(GetWorkshopServiceByIdUseCase),
            typeof(StockService),
            typeof(CreateStockUseCase),
            typeof(EntryStockUseCase),
            typeof(ConsumeStockUseCase),
            typeof(AdjustStockUseCase),
            typeof(ListStocksUseCase),
            typeof(GetStockByIdUseCase),
            typeof(CreateMechanicUseCase),
            typeof(UpdateMechanicUseCase),
            typeof(DeleteMechanicUseCase),
            typeof(ListMechanicsUseCase),
            typeof(GetMechanicByIdUseCase),
            typeof(GetWorkshopServiceExecutionTimesUseCase),
            typeof(BudgetService),
        ];

        foreach (var serviceType in expectedServices)
        {
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == serviceType && descriptor.Lifetime == ServiceLifetime.Scoped);
        }
    }
}

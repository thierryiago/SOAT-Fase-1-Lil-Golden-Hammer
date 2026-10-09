using Oficina.Domain.Budget;
using Oficina.Domain.OrderService;
using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.WorkshopServices;

namespace Oficina.Tests.Domain;

public sealed class BudgetFactoryTests
{
    private readonly BudgetFactory _factory = new();

    [Fact]
    public void Create_should_compose_snapshots_from_catalog_without_order_navigations()
    {
        var (order, part, service) = CreateOrder();
        var budgetId = Guid.NewGuid();
        var unrelatedPart = Part.Create("Pneu", "P-2", 500m);
        var unrelatedService = WorkshopService.Create("Alinhamento", "Descricao", 200m, 30);

        var budget = _factory.Create(budgetId, order, [unrelatedPart, part], [unrelatedService, service]);

        Assert.Equal(budgetId, budget.Id);
        Assert.Equal(order.CustomerId, budget.CustomerId);
        Assert.Equal(order.Id, budget.ServiceOrderId);
        Assert.Null(budget.IsApproved);
        Assert.Equal(130m, budget.TotalValue);
        var budgetPart = Assert.Single(budget.Parts);
        Assert.Equal(budgetId, budgetPart.BudgetId);
        Assert.Equal(part.Id, budgetPart.PartId);
        Assert.Equal(part.Name, budgetPart.PartName);
        Assert.Equal(10m, budgetPart.UnitPrice);
        Assert.Equal(3, budgetPart.Quantity);
        Assert.Same(part, budgetPart.Part);
        var budgetService = Assert.Single(budget.WorkshopServices);
        Assert.Equal(budgetId, budgetService.BudgetId);
        Assert.Equal(service.Id, budgetService.WorkshopServiceId);
        Assert.Equal(service.Name, budgetService.WorkshopServiceName);
        Assert.Equal(100m, budgetService.UnitPrice);
        Assert.Same(service, budgetService.WorkshopService);
        Assert.Null(Assert.Single(order.Parts).Part);
        Assert.Null(Assert.Single(order.WorkshopServices).WorkshopService);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_should_reject_missing_part_in_empty_or_partial_catalog(bool partiallyLoaded)
    {
        var (order, missingPart, service) = CreateOrder();
        var loadedPart = Part.Create("Pneu", "P-2", 500m);
        order.AddPart(ServiceOrderPart.Create(loadedPart.Id, order.Id, 1));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _factory.Create(Guid.NewGuid(), order, partiallyLoaded ? [loadedPart] : [], [service]));

        var missingIds = partiallyLoaded ? $"{missingPart.Id}" : $"{missingPart.Id}, {loadedPart.Id}";
        Assert.Equal($"Parts '{missingIds}' were not found.", exception.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_should_reject_missing_service_in_empty_or_partial_catalog(bool partiallyLoaded)
    {
        var (order, part, missingService) = CreateOrder();
        var loadedService = WorkshopService.Create("Alinhamento", "Descricao", 200m, 30);
        order.AddWorkshopService(ServiceOrderWorkshop.Create(order.Id, loadedService.Id));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _factory.Create(Guid.NewGuid(), order, [part], partiallyLoaded ? [loadedService] : []));

        var missingIds = partiallyLoaded ? $"{missingService.Id}" : $"{missingService.Id}, {loadedService.Id}";
        Assert.Equal($"Workshop services '{missingIds}' were not found.", exception.Message);
    }

    [Fact]
    public void Create_should_validate_missing_parts_before_missing_services()
    {
        var (order, part, _) = CreateOrder();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _factory.Create(Guid.NewGuid(), order, [], []));

        Assert.Equal($"Parts '{part.Id}' were not found.", exception.Message);
    }

    [Fact]
    public void Create_should_require_order_service_before_validating_catalog()
    {
        var (order, _, _) = CreateOrder();
        order.Update(null, null, null, null, []);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _factory.Create(Guid.NewGuid(), order, [], []));

        Assert.Equal("The service order must have at least one workshop service to open a budget.", exception.Message);
    }

    [Fact]
    public void Create_should_allow_order_without_parts()
    {
        var (order, _, service) = CreateOrder();
        order.Update(null, null, null, [], null);

        var budget = _factory.Create(Guid.NewGuid(), order, [], [service]);

        Assert.Empty(budget.Parts);
        Assert.Equal(100m, budget.TotalValue);
    }

    [Fact]
    public void Create_should_preserve_snapshots_after_catalog_changes()
    {
        var (order, part, service) = CreateOrder();
        var budget = _factory.Create(Guid.NewGuid(), order, [part], [service]);

        part.Update("Filtro novo", part.Code, 50m, part.Kind);
        service.Update("Servico novo", service.Description, 200m, 60);

        Assert.Equal("Filtro", Assert.Single(budget.Parts).PartName);
        Assert.Equal(10m, Assert.Single(budget.Parts).UnitPrice);
        Assert.Equal("Troca de oleo", Assert.Single(budget.WorkshopServices).WorkshopServiceName);
        Assert.Equal(100m, Assert.Single(budget.WorkshopServices).UnitPrice);
        Assert.Equal(130m, budget.TotalValue);
    }

    [Fact]
    public void Create_should_accept_loaded_inactive_catalog_entries()
    {
        var (order, part, service) = CreateOrder();
        part.Deactivate();
        service.Deactivate();

        var budget = _factory.Create(Guid.NewGuid(), order, [part], [service]);

        Assert.Equal(130m, budget.TotalValue);
    }

    [Theory]
    [InlineData("serviceOrder")]
    [InlineData("catalogParts")]
    [InlineData("catalogWorkshopServices")]
    public void Create_should_reject_null_arguments(string parameterName)
    {
        var (order, part, service) = CreateOrder();

        var exception = Assert.Throws<ArgumentNullException>(() => _factory.Create(
            Guid.NewGuid(),
            parameterName == "serviceOrder" ? null! : order,
            parameterName == "catalogParts" ? null! : [part],
            parameterName == "catalogWorkshopServices" ? null! : [service]));

        Assert.Equal(parameterName, exception.ParamName);
    }

    private static (ServiceOrder Order, Part Part, WorkshopService Service) CreateOrder()
    {
        var order = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Revisao");
        var part = Part.Create("Filtro", "P-1", 10m);
        var service = WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30);
        order.AddPart(ServiceOrderPart.Create(part.Id, order.Id, 3));
        order.AddWorkshopService(ServiceOrderWorkshop.Create(order.Id, service.Id));
        return (order, part, service);
    }
}

using Oficina.Application.Budgets.UseCases;
using Oficina.Domain.Budget;
using Oficina.Domain.ServiceOrders;
using static Oficina.Tests.Application.BudgetUseCaseTestFixture;

namespace Oficina.Tests.Application;

public sealed class OpenBudgetFromServiceOrderUseCaseTests
{
    [Fact]
    public async Task OpenFromServiceOrderAsync_should_throw_when_service_order_does_not_exist()
    {
        var useCase = CreateUseCase(
            new FakeBudgetRepository(), new FakeServiceOrderRepository(), new FakePartRepository(), new FakeWorkshopServiceRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.OpenFromServiceOrderAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task OpenFromServiceOrderAsync_should_throw_when_service_order_has_no_workshop_services()
    {
        var serviceOrders = new FakeServiceOrderRepository();
        var serviceOrder = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Revisao");
        await serviceOrders.AddAsync(serviceOrder, CancellationToken.None);
        var useCase = CreateUseCase(new FakeBudgetRepository(), serviceOrders, new FakePartRepository(), new FakeWorkshopServiceRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.OpenFromServiceOrderAsync(serviceOrder.Id, CancellationToken.None));
    }

    [Fact]
    public async Task OpenFromServiceOrderAsync_should_throw_when_a_referenced_part_no_longer_exists()
    {
        var serviceOrders = new FakeServiceOrderRepository();
        var (serviceOrder, part, workshopService) = CreateServiceOrderWithItems();
        await serviceOrders.AddAsync(serviceOrder, CancellationToken.None);
        var workshopServices = new FakeWorkshopServiceRepository();
        await workshopServices.AddAsync(workshopService, CancellationToken.None);
        var partsRepository = new FakePartRepository();
        var budgets = new FakeBudgetRepository();
        var useCase = CreateUseCase(budgets, serviceOrders, partsRepository, workshopServices);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.OpenFromServiceOrderAsync(serviceOrder.Id, CancellationToken.None));

        Assert.Equal($"Parts '{part.Id}' were not found.", exception.Message);
        Assert.Empty(await budgets.ListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task OpenFromServiceOrderAsync_should_not_persist_when_a_referenced_service_is_missing()
    {
        var serviceOrders = new FakeServiceOrderRepository();
        var (serviceOrder, part, workshopService) = CreateServiceOrderWithItems();
        await serviceOrders.AddAsync(serviceOrder, CancellationToken.None);
        var parts = new FakePartRepository();
        await parts.AddAsync(part, CancellationToken.None);
        var budgets = new FakeBudgetRepository();
        var useCase = CreateUseCase(budgets, serviceOrders, parts, new FakeWorkshopServiceRepository());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.OpenFromServiceOrderAsync(serviceOrder.Id, CancellationToken.None));

        Assert.Equal($"Workshop services '{workshopService.Id}' were not found.", exception.Message);
        Assert.Empty(await budgets.ListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task OpenFromServiceOrderAsync_should_open_budget_and_calculate_total_value()
    {
        var serviceOrders = new FakeServiceOrderRepository();
        var (serviceOrder, part, workshopService) = CreateServiceOrderWithItems();
        await serviceOrders.AddAsync(serviceOrder, CancellationToken.None);
        var partsRepository = new FakePartRepository();
        await partsRepository.AddAsync(part, CancellationToken.None);
        var workshopServices = new FakeWorkshopServiceRepository();
        await workshopServices.AddAsync(workshopService, CancellationToken.None);
        var budgets = new FakeBudgetRepository();
        var useCase = CreateUseCase(budgets, serviceOrders, partsRepository, workshopServices);

        var response = await useCase.OpenFromServiceOrderAsync(serviceOrder.Id, CancellationToken.None);

        Assert.Equal(serviceOrder.CustomerId, response.CustomerId);
        Assert.Equal(serviceOrder.Id, response.ServiceOrderId);
        Assert.Equal(130m, response.TotalValue);
        Assert.Single(response.Parts);
        Assert.Single(response.WorkshopServices);
        Assert.NotNull(await budgets.GetByIdAsync(response.Id, CancellationToken.None));
    }

    [Fact]
    public async Task OpenFromServiceOrderAsync_should_preserve_previous_budget_and_create_a_new_version()
    {
        var serviceOrders = new FakeServiceOrderRepository();
        var (serviceOrder, part, workshopService) = CreateServiceOrderWithItems();
        await serviceOrders.AddAsync(serviceOrder, CancellationToken.None);
        var partsRepository = new FakePartRepository();
        await partsRepository.AddAsync(part, CancellationToken.None);
        var workshopServices = new FakeWorkshopServiceRepository();
        await workshopServices.AddAsync(workshopService, CancellationToken.None);
        var budgets = new FakeBudgetRepository();
        var useCase = CreateUseCase(budgets, serviceOrders, partsRepository, workshopServices);

        var first = await useCase.OpenFromServiceOrderAsync(serviceOrder.Id, CancellationToken.None);
        SetCreatedAt((await budgets.GetByIdAsync(first.Id, CancellationToken.None))!, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        await new SetBudgetApprovalByServiceOrderUseCase(budgets).SetApprovalByServiceOrderAsync(serviceOrder.Id, true, CancellationToken.None);
        part.Update("Filtro atualizado", part.Code, 20m, part.Kind);
        workshopService.Update("Serviço atualizado", workshopService.Description, 200m, 30);
        var second = await useCase.OpenFromServiceOrderAsync(serviceOrder.Id, CancellationToken.None);
        SetCreatedAt((await budgets.GetByIdAsync(second.Id, CancellationToken.None))!, new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero));

        var storedBudgets = await budgets.ListAsync(CancellationToken.None);
        Assert.Equal(2, storedBudgets.Count);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(130m, (await budgets.GetByIdAsync(first.Id, CancellationToken.None))!.TotalValue);
        Assert.Equal("Filtro", Assert.Single((await budgets.GetByIdAsync(first.Id, CancellationToken.None))!.Parts).PartName);
        Assert.Equal("Troca de oleo", Assert.Single((await budgets.GetByIdAsync(first.Id, CancellationToken.None))!.WorkshopServices).WorkshopServiceName);
        Assert.Equal(260m, second.TotalValue);
        Assert.Equal("Filtro atualizado", Assert.Single(second.Parts).PartName);
        Assert.Equal("Serviço atualizado", Assert.Single(second.WorkshopServices).WorkshopServiceName);
        Assert.True((await budgets.GetByIdAsync(first.Id, CancellationToken.None))!.IsApproved);
        Assert.Null((await budgets.GetByIdAsync(second.Id, CancellationToken.None))!.IsApproved);
        Assert.Equal(second.Id, (await budgets.GetByServiceOrderIdAsync(
            serviceOrder.Id,
            CancellationToken.None))!.Id);
    }

    private static OpenBudgetFromServiceOrderUseCase CreateUseCase(
        FakeBudgetRepository budgets,
        FakeServiceOrderRepository serviceOrders,
        FakePartRepository parts,
        FakeWorkshopServiceRepository workshopServices) =>
        new(budgets, serviceOrders, parts, workshopServices, new BudgetFactory());
    [Fact]
    public async Task OpenFromServiceOrderAsync_should_forward_token_to_all_repositories()
    {
        var budgets = new FakeBudgetRepository();
        var orders = new FakeServiceOrderRepository();
        var parts = new FakePartRepository();
        var workshopServices = new FakeWorkshopServiceRepository();
        var (order, part, workshopService) = CreateServiceOrderWithItems();
        await orders.AddAsync(order, CancellationToken.None);
        await parts.AddAsync(part, CancellationToken.None);
        await workshopServices.AddAsync(workshopService, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();

        var response = await CreateUseCase(budgets, orders, parts, workshopServices)
            .OpenFromServiceOrderAsync(order.Id, cancellation.Token);

        Assert.Equal(cancellation.Token, Assert.Single(orders.Tokens));
        Assert.Equal(cancellation.Token, Assert.Single(parts.Tokens));
        Assert.Equal(cancellation.Token, Assert.Single(workshopServices.Tokens));
        Assert.Equal(cancellation.Token, Assert.Single(budgets.Tokens));
        AssertBudgetResponse((await budgets.GetByIdAsync(response.Id, CancellationToken.None))!, response);
    }
}

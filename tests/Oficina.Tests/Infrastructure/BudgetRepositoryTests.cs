using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Budget;
using Oficina.Domain.Customers;
using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.WorkshopServices;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Tests.Infrastructure;

public sealed class BudgetRepositoryTests
{
    [Fact]
    public async Task AddAsync_and_ListAsync_should_persist_and_return_budget_with_items()
    {
        await using var context = CreateContext();
        var customer = Customer.Create("Ana Silva", "ana@email.com", "11999990000", "11144477735");
        context.Customers.Add(customer);
        var part = Part.Create("Filtro", "COD-001", 10m, EnumPartKind.Part);
        context.Parts.Add(part);
        var workshopService = WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30);
        context.WorkshopServices.Add(workshopService);
        var serviceOrder = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "Revisao");
        context.ServiceOrders.Add(serviceOrder);
        await context.SaveChangesAsync();

        var budgetId = Guid.NewGuid();
        var budgetPart = BudgetParts.Create(budgetId, part.Id, part.Name, part.UnitPrice, 2);
        var budgetWorkshopService = BudgetWorkshopServices.Create(
            budgetId, workshopService.Id, workshopService.Name, workshopService.UnitPrice);
        var budget = Budget.Open(budgetId, customer.Id, serviceOrder.Id, [budgetPart], [budgetWorkshopService]);

        var repository = new BudgetRepository(context);
        await repository.AddAsync(budget, CancellationToken.None);

        var result = await repository.ListAsync(CancellationToken.None);

        var resultBudget = Assert.Single(result);
        Assert.Equal(budget.Id, resultBudget.Id);
        Assert.Single(resultBudget.Parts);
        Assert.Equal("Filtro", resultBudget.Parts.First().PartName);
        Assert.Equal(10m, resultBudget.Parts.First().UnitPrice);
        Assert.Equal("Filtro", resultBudget.Parts.First().Part!.Name);
        Assert.Single(resultBudget.WorkshopServices);
        Assert.Equal("Troca de oleo", resultBudget.WorkshopServices.First().WorkshopServiceName);
        Assert.Equal(100m, resultBudget.WorkshopServices.First().UnitPrice);
        Assert.Equal("Troca de oleo", resultBudget.WorkshopServices.First().WorkshopService!.Name);
    }

    [Fact]
    public async Task GetByIdAsync_should_return_null_when_budget_does_not_exist()
    {
        await using var context = CreateContext();
        var repository = new BudgetRepository(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByServiceOrderIdAsync_should_return_latest_budget_version()
    {
        await using var context = CreateContext();
        var customer = Customer.Create("Ana Silva", "ana@email.com", "11999990000", "11144477735");
        context.Customers.Add(customer);
        var workshopService = WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30);
        context.WorkshopServices.Add(workshopService);
        var serviceOrder = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "Revisao");
        context.ServiceOrders.Add(serviceOrder);
        await context.SaveChangesAsync();

        var firstId = Guid.NewGuid();
        var first = Budget.Open(
            firstId,
            customer.Id,
            serviceOrder.Id,
            [],
            [BudgetWorkshopServices.Create(firstId, workshopService.Id, workshopService.Name, workshopService.UnitPrice)]);
        var secondId = Guid.NewGuid();
        var second = Budget.Open(
            secondId,
            customer.Id,
            serviceOrder.Id,
            [],
            [BudgetWorkshopServices.Create(secondId, workshopService.Id, workshopService.Name, workshopService.UnitPrice)]);
        var repository = new BudgetRepository(context);

        await repository.AddAsync(first, CancellationToken.None);
        await repository.AddAsync(second, CancellationToken.None);

        var result = await repository.GetByServiceOrderIdAsync(serviceOrder.Id, CancellationToken.None);
        Assert.Equal(second.Id, result!.Id);
        Assert.Equal(2, (await repository.ListAsync(CancellationToken.None)).Count);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"oficina-budgets-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }
}

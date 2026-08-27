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
        var budgetPart = BudgetParts.Create(budgetId, part.Id, 2);
        var budgetWorkshopService = BudgetWorkshopServices.Create(budgetId, workshopService.Id);
        var budget = Budget.Open(budgetId, customer.Id, serviceOrder.Id, [budgetPart], [budgetWorkshopService]);

        var repository = new BudgetRepository(context);
        await repository.AddAsync(budget, CancellationToken.None);

        var result = await repository.ListAsync(CancellationToken.None);

        Assert.Collection(result, item =>
        {
            Assert.Equal(budget.Id, item.Id);
            Assert.Single(item.Parts);
            Assert.Equal("Filtro", item.Parts.First().Part!.Name);
            Assert.Single(item.WorkshopServices);
            Assert.Equal("Troca de oleo", item.WorkshopServices.First().WorkshopService!.Name);
        });
    }

    [Fact]
    public async Task GetByIdAsync_should_return_null_when_budget_does_not_exist()
    {
        await using var context = CreateContext();
        var repository = new BudgetRepository(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"oficina-budgets-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }
}

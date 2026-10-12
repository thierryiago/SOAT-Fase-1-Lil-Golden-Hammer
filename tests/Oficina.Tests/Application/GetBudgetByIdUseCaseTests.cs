using Oficina.Application.Budgets.UseCases.Queries;
using static Oficina.Tests.Application.BudgetUseCaseTestFixture;

namespace Oficina.Tests.Application;

public sealed class GetBudgetByIdUseCaseTests
{
    [Fact]
    public async Task GetByIdAsync_should_return_null_when_budget_does_not_exist()
    {
        var useCase = new GetBudgetByIdUseCase(new FakeBudgetRepository());

        var result = await useCase.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_should_map_all_snapshot_fields_and_forward_token()
    {
        var budgets = new FakeBudgetRepository();
        var (order, part, workshopService) = CreateServiceOrderWithItems();
        var budget = OpenBudget(order, part, workshopService);
        budget.SetApproval(true);
        await budgets.AddAsync(budget, CancellationToken.None);
        part.Update("Novo filtro", part.Code, 90m, part.Kind);
        workshopService.Update("Novo serviço", workshopService.Description, 800m, 30);
        budgets.Tokens.Clear();
        using var cancellation = new CancellationTokenSource();

        var response = await new GetBudgetByIdUseCase(budgets).GetByIdAsync(budget.Id, cancellation.Token);

        Assert.NotNull(response);
        AssertBudgetResponse(budget, response);
        Assert.Equal("Filtro", Assert.Single(response.Parts).PartName);
        Assert.Equal(10m, Assert.Single(response.Parts).UnitPrice);
        Assert.Equal("Troca de oleo", Assert.Single(response.WorkshopServices).WorkshopServiceName);
        Assert.Equal(100m, Assert.Single(response.WorkshopServices).UnitPrice);
        Assert.Equal(cancellation.Token, Assert.Single(budgets.Tokens));
    }
}

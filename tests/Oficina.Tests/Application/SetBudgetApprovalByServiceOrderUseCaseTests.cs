using Oficina.Application.Budgets.UseCases;
using static Oficina.Tests.Application.BudgetUseCaseTestFixture;

namespace Oficina.Tests.Application;

public sealed class SetBudgetApprovalByServiceOrderUseCaseTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetApprovalByServiceOrderAsync_should_update_budget(bool isApproved)
    {
        var budgets = new FakeBudgetRepository();
        var (serviceOrder, part, workshopService) = CreateServiceOrderWithItems();
        var budget = OpenBudget(serviceOrder, part, workshopService);
        await budgets.AddAsync(budget, CancellationToken.None);
        var useCase = new SetBudgetApprovalByServiceOrderUseCase(budgets);

        await useCase.SetApprovalByServiceOrderAsync(
            serviceOrder.Id,
            isApproved,
            CancellationToken.None);

        var updatedBudget = await budgets.GetByServiceOrderIdAsync(
            serviceOrder.Id,
            CancellationToken.None);
        Assert.Equal(isApproved, updatedBudget!.IsApproved);
    }

    [Fact]
    public async Task SetApprovalByServiceOrderAsync_should_keep_decision_when_budget_was_already_decided()
    {
        var budgets = new FakeBudgetRepository();
        var (serviceOrder, part, workshopService) = CreateServiceOrderWithItems();
        var budget = OpenBudget(serviceOrder, part, workshopService);
        await budgets.AddAsync(budget, CancellationToken.None);
        var useCase = new SetBudgetApprovalByServiceOrderUseCase(budgets);
        await new SetBudgetApprovalByBudgetIdUseCase(budgets).SetApprovalByBudgetIdAsync(budget.Id, true, CancellationToken.None);

        await useCase.SetApprovalByServiceOrderAsync(serviceOrder.Id, false, CancellationToken.None);
        var stored = await budgets.GetByIdAsync(budget.Id, CancellationToken.None);

        Assert.True(stored!.IsApproved);
    }

    [Fact]
    public async Task SetApprovalByServiceOrderAsync_should_throw_when_budget_is_missing()
    {
        var budgets = new FakeBudgetRepository();
        var useCase = new SetBudgetApprovalByServiceOrderUseCase(budgets);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.SetApprovalByServiceOrderAsync(Guid.NewGuid(), true, CancellationToken.None));

        Assert.Equal("Budget was not found for the service order.", exception.Message);
        Assert.Equal(0, budgets.UpdateCount);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task SetApprovalByServiceOrderAsync_should_preserve_existing_decision_without_writing(
        bool previousDecision, bool requestedDecision)
    {
        var budgets = new FakeBudgetRepository();
        var (order, part, workshopService) = CreateServiceOrderWithItems();
        var budget = OpenBudget(order, part, workshopService);
        budget.SetApproval(previousDecision);
        await budgets.AddAsync(budget, CancellationToken.None);

        await new SetBudgetApprovalByServiceOrderUseCase(budgets)
            .SetApprovalByServiceOrderAsync(order.Id, requestedDecision, CancellationToken.None);

        Assert.Equal(previousDecision, budget.IsApproved);
        Assert.Equal(0, budgets.UpdateCount);
    }

    [Fact]
    public async Task SetApprovalByServiceOrderAsync_should_forward_token_to_lookup_and_update()
    {
        var budgets = new FakeBudgetRepository();
        var (order, part, workshopService) = CreateServiceOrderWithItems();
        await budgets.AddAsync(OpenBudget(order, part, workshopService), CancellationToken.None);
        budgets.Tokens.Clear();
        using var cancellation = new CancellationTokenSource();

        await new SetBudgetApprovalByServiceOrderUseCase(budgets)
            .SetApprovalByServiceOrderAsync(order.Id, true, cancellation.Token);

        Assert.Equal(new[] { cancellation.Token, cancellation.Token }, budgets.Tokens);
        Assert.Equal(1, budgets.UpdateCount);
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetApprovalByServiceOrderAsync_should_decide_only_latest_version(bool isApproved)
    {
        var budgets = new FakeBudgetRepository();
        var (order, part, workshopService) = CreateServiceOrderWithItems();
        var previous = OpenBudget(order, part, workshopService);
        var latest = OpenBudget(order, part, workshopService);
        SetCreatedAt(previous, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        SetCreatedAt(latest, previous.CreatedAt.AddDays(1));
        await budgets.AddAsync(latest, CancellationToken.None);
        await budgets.AddAsync(previous, CancellationToken.None);

        await new SetBudgetApprovalByServiceOrderUseCase(budgets)
            .SetApprovalByServiceOrderAsync(order.Id, isApproved, CancellationToken.None);

        Assert.Null(previous.IsApproved);
        Assert.Equal(isApproved, latest.IsApproved);
        Assert.Equal(1, budgets.UpdateCount);
    }
}

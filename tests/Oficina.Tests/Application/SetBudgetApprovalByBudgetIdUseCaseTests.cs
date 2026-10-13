using Oficina.Application.Budgets.UseCases;
using static Oficina.Tests.Application.BudgetUseCaseTestFixture;

namespace Oficina.Tests.Application;

public sealed class SetBudgetApprovalByBudgetIdUseCaseTests
{
    [Fact]
    public async Task SetApprovalByBudgetIdAsync_should_approve_budget()
    {
        var budgets = new FakeBudgetRepository();
        var (serviceOrder, part, workshopService) = CreateServiceOrderWithItems();
        var budget = OpenBudget(serviceOrder, part, workshopService);
        await budgets.AddAsync(budget, CancellationToken.None);
        var useCase = new SetBudgetApprovalByBudgetIdUseCase(budgets);

        var result = await useCase.SetApprovalByBudgetIdAsync(budget.Id, true, CancellationToken.None);
        var stored = await budgets.GetByIdAsync(budget.Id, CancellationToken.None);

        Assert.True(result.IsApproved);
        Assert.True(stored!.IsApproved);
        Assert.Equal(budget.ServiceOrderId, result.ServiceOrderId);
    }

    [Fact]
    public async Task SetApprovalByBudgetIdAsync_should_reject_budget()
    {
        var budgets = new FakeBudgetRepository();
        var (serviceOrder, part, workshopService) = CreateServiceOrderWithItems();
        var budget = OpenBudget(serviceOrder, part, workshopService);
        await budgets.AddAsync(budget, CancellationToken.None);
        var useCase = new SetBudgetApprovalByBudgetIdUseCase(budgets);

        var result = await useCase.SetApprovalByBudgetIdAsync(budget.Id, false, CancellationToken.None);
        var stored = await budgets.GetByIdAsync(budget.Id, CancellationToken.None);

        Assert.False(result.IsApproved);
        Assert.False(stored!.IsApproved);
    }

    [Fact]
    public async Task SetApprovalByBudgetIdAsync_should_throw_when_budget_is_not_found()
    {
        var useCase = new SetBudgetApprovalByBudgetIdUseCase(new FakeBudgetRepository());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.SetApprovalByBudgetIdAsync(Guid.NewGuid(), true, CancellationToken.None));

        Assert.Equal("Budget was not found.", exception.Message);
    }

    [Fact]
    public async Task SetApprovalByBudgetIdAsync_should_throw_when_budget_is_already_approved()
    {
        var budgets = new FakeBudgetRepository();
        var (serviceOrder, part, workshopService) = CreateServiceOrderWithItems();
        var budget = OpenBudget(serviceOrder, part, workshopService);
        await budgets.AddAsync(budget, CancellationToken.None);
        var useCase = new SetBudgetApprovalByBudgetIdUseCase(budgets);
        await useCase.SetApprovalByBudgetIdAsync(budget.Id, true, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.SetApprovalByBudgetIdAsync(budget.Id, true, CancellationToken.None));

        Assert.Equal("Budget is already approved.", exception.Message);
    }

    [Fact]
    public async Task SetApprovalByBudgetIdAsync_should_throw_when_budget_is_already_rejected()
    {
        var budgets = new FakeBudgetRepository();
        var (serviceOrder, part, workshopService) = CreateServiceOrderWithItems();
        var budget = OpenBudget(serviceOrder, part, workshopService);
        await budgets.AddAsync(budget, CancellationToken.None);
        var useCase = new SetBudgetApprovalByBudgetIdUseCase(budgets);
        await useCase.SetApprovalByBudgetIdAsync(budget.Id, false, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.SetApprovalByBudgetIdAsync(budget.Id, false, CancellationToken.None));

        Assert.Equal("Budget is already rejected.", exception.Message);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task SetApprovalByBudgetIdAsync_should_preserve_decision_and_throw_without_writing(
        bool previousDecision, bool requestedDecision)
    {
        var budgets = new FakeBudgetRepository();
        var (order, part, workshopService) = CreateServiceOrderWithItems();
        var budget = OpenBudget(order, part, workshopService);
        budget.SetApproval(previousDecision);
        await budgets.AddAsync(budget, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SetBudgetApprovalByBudgetIdUseCase(budgets)
                .SetApprovalByBudgetIdAsync(budget.Id, requestedDecision, CancellationToken.None));

        Assert.Equal(previousDecision ? "Budget is already approved." : "Budget is already rejected.", exception.Message);
        Assert.Equal(previousDecision, budget.IsApproved);
        Assert.Equal(0, budgets.UpdateCount);
    }

    [Fact]
    public async Task SetApprovalByBudgetIdAsync_should_return_mapped_response_and_forward_token()
    {
        var budgets = new FakeBudgetRepository();
        var (order, part, workshopService) = CreateServiceOrderWithItems();
        var budget = OpenBudget(order, part, workshopService);
        await budgets.AddAsync(budget, CancellationToken.None);
        budgets.Tokens.Clear();
        using var cancellation = new CancellationTokenSource();

        var response = await new SetBudgetApprovalByBudgetIdUseCase(budgets)
            .SetApprovalByBudgetIdAsync(budget.Id, false, cancellation.Token);

        AssertBudgetResponse(budget, response);
        Assert.False(response.IsApproved);
        Assert.Equal(new[] { cancellation.Token, cancellation.Token }, budgets.Tokens);
        Assert.Equal(1, budgets.UpdateCount);
    }
}

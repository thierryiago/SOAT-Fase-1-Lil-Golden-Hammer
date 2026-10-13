using Oficina.Application.Budgets.UseCases.Queries;
using Oficina.Application.Common;
using static Oficina.Tests.Application.BudgetUseCaseTestFixture;

namespace Oficina.Tests.Application;

public sealed class ListBudgetsUseCaseTests
{
    [Fact]
    public async Task ListAsync_should_return_registered_budgets()
    {
        var budgets = new FakeBudgetRepository();
        var (serviceOrder, part, workshopService) = CreateServiceOrderWithItems();
        var budget = OpenBudget(serviceOrder, part, workshopService);
        await budgets.AddAsync(budget, CancellationToken.None);
        var useCase = new ListBudgetsUseCase(budgets);

        var result = await useCase.ListAsync(new PageRequest(), CancellationToken.None);

        Assert.Equal(budget.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task ListAsync_should_return_empty_page_when_no_budgets_exist()
    {
        var result = await new ListBudgetsUseCase(new FakeBudgetRepository())
            .ListAsync(new PageRequest(), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalItems);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task ListAsync_should_page_budgets_from_newest_to_oldest_and_forward_token()
    {
        var budgets = new FakeBudgetRepository();
        var (order, part, workshopService) = CreateServiceOrderWithItems();
        var oldest = OpenBudget(order, part, workshopService);
        var middle = OpenBudget(order, part, workshopService);
        var newest = OpenBudget(order, part, workshopService);
        SetCreatedAt(oldest, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        SetCreatedAt(middle, oldest.CreatedAt.AddDays(1));
        SetCreatedAt(newest, oldest.CreatedAt.AddDays(2));
        await budgets.AddAsync(middle, CancellationToken.None);
        await budgets.AddAsync(oldest, CancellationToken.None);
        await budgets.AddAsync(newest, CancellationToken.None);
        budgets.Tokens.Clear();
        using var cancellation = new CancellationTokenSource();
        var useCase = new ListBudgetsUseCase(budgets);

        var firstPage = await useCase.ListAsync(new PageRequest(PageSize: 2), cancellation.Token);
        var secondPage = await useCase.ListAsync(new PageRequest(Page: 2, PageSize: 2), cancellation.Token);

        Assert.Equal(new[] { newest.Id, middle.Id }, firstPage.Items.Select(budget => budget.Id));
        Assert.Equal(oldest.Id, Assert.Single(secondPage.Items).Id);
        Assert.Equal(3, secondPage.TotalItems);
        Assert.Equal(2, secondPage.TotalPages);
        Assert.Equal(2, secondPage.Page);
        Assert.Equal(2, secondPage.PageSize);
        Assert.Equal(new[] { cancellation.Token, cancellation.Token }, budgets.Tokens);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task ListAsync_should_reject_invalid_pagination(int page, int pageSize)
    {
        var useCase = new ListBudgetsUseCase(new FakeBudgetRepository());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            useCase.ListAsync(new PageRequest(Page: page, PageSize: pageSize), CancellationToken.None));
    }
}

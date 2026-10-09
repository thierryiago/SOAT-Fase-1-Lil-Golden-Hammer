using Oficina.Application.OrderServiceHistory;
using Oficina.Application.OrderServiceHistory.UseCases;
using Oficina.Application.OrderServiceHistory.UseCases.Queries;
using Oficina.Domain.OrderServiceHistory;

namespace Oficina.Tests.Application;

public sealed class ServiceOrderHistoryUseCasesTests
{
    [Fact]
    public async Task FindAllAsync_should_return_all_history_entries_with_all_fields()
    {
        var repository = new FakeServiceOrderHistoryRepository();
        var received = HistoryEntry(Guid.NewGuid(), "Received");
        var diagnosis = HistoryEntry(Guid.NewGuid(), "InDiagnosis");
        repository.Histories.AddRange([received, diagnosis]);
        var useCase = new ListServiceOrderHistoryUseCase(repository);

        var result = await useCase.FindAllAsync(CancellationToken.None);

        Assert.Equal(new[] { ResponseFor(received), ResponseFor(diagnosis) }, result);
        Assert.Equal(1, repository.ListCalls);
    }

    [Fact]
    public async Task FindAllAsync_should_return_empty_when_no_history_exists()
    {
        var useCase = new ListServiceOrderHistoryUseCase(new FakeServiceOrderHistoryRepository());

        var result = await useCase.FindAllAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task FindAllAsync_should_forward_cancellation_token()
    {
        var repository = new FakeServiceOrderHistoryRepository();
        var useCase = new ListServiceOrderHistoryUseCase(repository);
        using var cancellation = new CancellationTokenSource();

        await useCase.FindAllAsync(cancellation.Token);

        Assert.Equal(cancellation.Token, repository.LastCancellationToken);
    }

    [Fact]
    public async Task FindByServiceOrderAsync_should_return_only_matching_entries_with_all_fields()
    {
        var repository = new FakeServiceOrderHistoryRepository();
        var serviceOrderId = Guid.NewGuid();
        var received = HistoryEntry(serviceOrderId, "Received");
        repository.Histories.AddRange([received, HistoryEntry(Guid.NewGuid(), "Received")]);
        var useCase = new GetServiceOrderHistoryByServiceOrderUseCase(repository);

        var result = await useCase.FindByServiceOrderAsync(serviceOrderId, CancellationToken.None);

        Assert.Equal(ResponseFor(received), Assert.Single(result));
        Assert.Equal(serviceOrderId, repository.LastServiceOrderId);
        Assert.Equal(1, repository.FindCalls);
    }

    [Fact]
    public async Task FindByServiceOrderAsync_should_return_empty_when_no_matching_history_exists()
    {
        var repository = new FakeServiceOrderHistoryRepository();
        repository.Histories.Add(HistoryEntry(Guid.NewGuid(), "Received"));
        var useCase = new GetServiceOrderHistoryByServiceOrderUseCase(repository);

        var result = await useCase.FindByServiceOrderAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task FindByServiceOrderAsync_should_reject_empty_id_without_calling_repository()
    {
        var repository = new FakeServiceOrderHistoryRepository();
        var useCase = new GetServiceOrderHistoryByServiceOrderUseCase(repository);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.FindByServiceOrderAsync(Guid.Empty, CancellationToken.None));

        Assert.Equal("serviceOrderId", exception.ParamName);
        Assert.StartsWith("Service order id is required.", exception.Message);
        Assert.Equal(0, repository.FindCalls);
    }

    [Fact]
    public async Task FindByServiceOrderAsync_should_forward_cancellation_token()
    {
        var repository = new FakeServiceOrderHistoryRepository();
        var useCase = new GetServiceOrderHistoryByServiceOrderUseCase(repository);
        using var cancellation = new CancellationTokenSource();

        await useCase.FindByServiceOrderAsync(Guid.NewGuid(), cancellation.Token);

        Assert.Equal(cancellation.Token, repository.LastCancellationToken);
    }

    [Fact]
    public async Task CreateAsync_should_reject_empty_id_without_calling_repository()
    {
        var repository = new FakeServiceOrderHistoryRepository();
        var useCase = new CreateServiceOrderHistoryUseCase(repository);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.CreateAsync(Guid.Empty, "Received", CancellationToken.None));

        Assert.Equal("serviceOrderId", exception.ParamName);
        Assert.StartsWith("Service order id is required.", exception.Message);
        Assert.Equal(0, repository.AddCalls);
        Assert.Empty(repository.Histories);
    }

    [Fact]
    public async Task CreateAsync_should_persist_and_return_history_entry()
    {
        var repository = new FakeServiceOrderHistoryRepository();
        var useCase = new CreateServiceOrderHistoryUseCase(repository);
        var serviceOrderId = Guid.NewGuid();

        var history = await useCase.CreateAsync(serviceOrderId, "Received", CancellationToken.None);

        Assert.Same(history, Assert.Single(repository.Histories));
        Assert.Equal(1, repository.AddCalls);
        Assert.Equal(serviceOrderId, history.OrderServiceId);
        Assert.Equal("Received", history.StatusName);
        Assert.NotEqual(Guid.Empty, history.Id);
    }

    [Theory]
    [InlineData(null, "Unknown")]
    [InlineData("", "Unknown")]
    [InlineData("   ", "Unknown")]
    [InlineData(" Received ", "Received")]
    public async Task CreateAsync_should_preserve_domain_status_normalization(string? statusName, string expectedStatus)
    {
        var repository = new FakeServiceOrderHistoryRepository();
        var useCase = new CreateServiceOrderHistoryUseCase(repository);

        var history = await useCase.CreateAsync(Guid.NewGuid(), statusName, CancellationToken.None);

        Assert.Equal(expectedStatus, history.StatusName);
        Assert.Same(history, Assert.Single(repository.Histories));
    }

    [Fact]
    public async Task CreateAsync_should_forward_cancellation_token()
    {
        var repository = new FakeServiceOrderHistoryRepository();
        var useCase = new CreateServiceOrderHistoryUseCase(repository);
        using var cancellation = new CancellationTokenSource();

        await useCase.CreateAsync(Guid.NewGuid(), "Received", cancellation.Token);

        Assert.Equal(cancellation.Token, repository.LastCancellationToken);
    }

    private static ServiceOrderHistory HistoryEntry(Guid serviceOrderId, string statusName) =>
        new(Guid.NewGuid(), serviceOrderId, statusName, new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

    private static ServiceOrderHistoryResponse ResponseFor(ServiceOrderHistory history) =>
        new(history.Id, history.OrderServiceId, history.StatusName, history.CreatedDate);

    private sealed class FakeServiceOrderHistoryRepository : IServiceOrderHistoryRepository
    {
        public List<ServiceOrderHistory> Histories { get; } = [];
        public int ListCalls { get; private set; }
        public int FindCalls { get; private set; }
        public int AddCalls { get; private set; }
        public Guid? LastServiceOrderId { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public Task<List<ServiceOrderHistory>> ListAsync(CancellationToken cancellationToken)
        {
            ListCalls++;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(Histories.ToList());
        }

        public Task<List<ServiceOrderHistory>> FindByServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken)
        {
            FindCalls++;
            LastServiceOrderId = serviceOrderId;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(Histories.Where(history => history.OrderServiceId == serviceOrderId).ToList());
        }

        public Task AddAsync(ServiceOrderHistory history, CancellationToken cancellationToken)
        {
            AddCalls++;
            LastCancellationToken = cancellationToken;
            Histories.Add(history);
            return Task.CompletedTask;
        }
    }
}

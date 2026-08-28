using Oficina.Application.OrderServiceHistory;
using Oficina.Domain.OrderServiceHistory;

namespace Oficina.Tests.Application;

public sealed class ServiceOrderHistoryServiceTests
{
    [Fact]
    public async Task FindAllAsync_should_return_all_history_entries()
    {
        var repository = new FakeServiceOrderHistoryRepository();
        var service = new ServiceOrderHistoryService(repository);
        await service.CreateAsync(Guid.NewGuid(), "Received", CancellationToken.None);
        await service.CreateAsync(Guid.NewGuid(), "InDiagnosis", CancellationToken.None);

        var result = await service.FindAllAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task FindByServiceOrderAsync_should_return_only_matching_entries()
    {
        var repository = new FakeServiceOrderHistoryRepository();
        var service = new ServiceOrderHistoryService(repository);
        var serviceOrderId = Guid.NewGuid();
        await service.CreateAsync(serviceOrderId, "Received", CancellationToken.None);
        await service.CreateAsync(Guid.NewGuid(), "Received", CancellationToken.None);

        var result = await service.FindByServiceOrderAsync(serviceOrderId, CancellationToken.None);

        Assert.Equal(serviceOrderId, Assert.Single(result).ServiceOrderId);
    }

    [Fact]
    public async Task FindByServiceOrderAsync_should_reject_empty_id()
    {
        var repository = new FakeServiceOrderHistoryRepository();
        var service = new ServiceOrderHistoryService(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.FindByServiceOrderAsync(Guid.Empty, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_should_reject_empty_id()
    {
        var repository = new FakeServiceOrderHistoryRepository();
        var service = new ServiceOrderHistoryService(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(Guid.Empty, "Received", CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_should_persist_history_entry()
    {
        var repository = new FakeServiceOrderHistoryRepository();
        var service = new ServiceOrderHistoryService(repository);
        var serviceOrderId = Guid.NewGuid();

        var history = await service.CreateAsync(serviceOrderId, "Received", CancellationToken.None);

        Assert.Equal(serviceOrderId, history.OrderServiceId);
        Assert.Equal("Received", history.StatusName);
    }

    private sealed class FakeServiceOrderHistoryRepository : IServiceOrderHistoryRepository
    {
        private readonly List<ServiceOrderHistory> _history = [];

        public Task<List<ServiceOrderHistory>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_history.ToList());

        public Task<List<ServiceOrderHistory>> FindByServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken) =>
            Task.FromResult(_history.Where(item => item.OrderServiceId == serviceOrderId).ToList());

        public Task AddAsync(ServiceOrderHistory history, CancellationToken cancellationToken)
        {
            _history.Add(history);
            return Task.CompletedTask;
        }
    }
}

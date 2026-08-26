using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Customers;
using Oficina.Domain.OrderServiceHistory;
using Oficina.Domain.ServiceOrders;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Tests.Infrastructure;

public sealed class ServiceOrderHistoryRepositoryTests
{
    [Fact]
    public async Task AddAsync_and_ListAsync_should_persist_and_return_history()
    {
        await using var context = CreateContext();
        var serviceOrder = await AddServiceOrderAsync(context);
        var repository = new ServiceOrderHistoryRepository(context);
        var history = ServiceOrderHistory.Create(serviceOrder.Id, "Received");

        await repository.AddAsync(history, CancellationToken.None);
        var result = await repository.ListAsync(CancellationToken.None);

        Assert.Collection(result, item => Assert.Equal(history.Id, item.Id));
    }

    [Fact]
    public async Task FindByServiceOrderAsync_should_return_only_matching_entries()
    {
        await using var context = CreateContext();
        var serviceOrderA = await AddServiceOrderAsync(context);
        var serviceOrderB = await AddServiceOrderAsync(context);
        var repository = new ServiceOrderHistoryRepository(context);
        await repository.AddAsync(ServiceOrderHistory.Create(serviceOrderA.Id, "Received"), CancellationToken.None);
        await repository.AddAsync(ServiceOrderHistory.Create(serviceOrderB.Id, "Received"), CancellationToken.None);

        var result = await repository.FindByServiceOrderAsync(serviceOrderA.Id, CancellationToken.None);

        Assert.Collection(result, item => Assert.Equal(serviceOrderA.Id, item.OrderServiceId));
    }

    private static async Task<ServiceOrder> AddServiceOrderAsync(AppDbContext context)
    {
        var customer = Customer.Create("Ana Silva", "ana@email.com", "11999990000", "12345678901");
        context.Customers.Add(customer);
        var serviceOrder = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "Revisao");
        context.ServiceOrders.Add(serviceOrder);
        await context.SaveChangesAsync();
        return serviceOrder;
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"oficina-serviceorderhistory-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }
}

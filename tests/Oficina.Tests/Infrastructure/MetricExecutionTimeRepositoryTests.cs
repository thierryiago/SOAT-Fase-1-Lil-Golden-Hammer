using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Customers;
using Oficina.Domain.OrderService;
using Oficina.Domain.OrderServiceHistory;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.WorkshopServices;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Tests.Infrastructure;

public sealed class MetricExecutionTimeRepositoryTests
{
    [Fact]
    public async Task GetAsync_should_aggregate_workshop_services_and_finalized_order_history()
    {
        await using var context = CreateContext();

        var customer = Customer.Create("Ana Silva", "ana@email.com", "11999990000", "12345678901");
        context.Customers.Add(customer);
        var workshopService = WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30);
        context.WorkshopServices.Add(workshopService);

        var serviceOrder = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "Revisao");
        var serviceOrderWorkshop = ServiceOrderWorkshop.Create(serviceOrder.Id, workshopService.Id);
        serviceOrder.Update(null, null, "Checklist ok", null, [serviceOrderWorkshop]);
        serviceOrder.UpdateStatus(); // -> Received
        serviceOrder.Update(Guid.NewGuid(), null, null, null, null);
        serviceOrder.UpdateStatus(); // -> InDiagnosis
        serviceOrder.UpdateStatus(); // -> AwaitingApproval (workshop service already set)
        serviceOrder.UpdateStatus(clientApproved: true); // -> InExecution
        serviceOrder.UpdateStatus(finalized: true); // Finalized
        context.ServiceOrders.Add(serviceOrder);

        context.ServiceOrderHistories.Add(ServiceOrderHistory.Create(serviceOrder.Id, "InExecution"));
        context.ServiceOrderHistories.Add(ServiceOrderHistory.Create(serviceOrder.Id, "Finalized"));
        await context.SaveChangesAsync();

        var repository = new MetricExecutionTimeRepository(context);

        var result = await repository.GetAsync(CancellationToken.None);

        Assert.Collection(result.WorkshopServices,
            item => Assert.Equal(workshopService.Id, item.WorkshopServiceId));
        Assert.Collection(result.ServiceOrders, item =>
        {
            Assert.Equal(serviceOrder.Id, item.ServiceOrderId);
            Assert.Single(item.WorkshopServices);
            Assert.Equal(2, item.Histories.Count);
        });
    }

    [Fact]
    public async Task GetAsync_should_ignore_orders_that_are_not_finalized()
    {
        await using var context = CreateContext();
        var customer = Customer.Create("Ana Silva", "ana@email.com", "11999990000", "12345678901");
        context.Customers.Add(customer);
        var serviceOrder = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "Revisao");
        context.ServiceOrders.Add(serviceOrder);
        await context.SaveChangesAsync();

        var repository = new MetricExecutionTimeRepository(context);

        var result = await repository.GetAsync(CancellationToken.None);

        Assert.Empty(result.ServiceOrders);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"oficina-metrics-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }
}

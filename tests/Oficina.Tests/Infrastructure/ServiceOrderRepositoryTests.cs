using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Customers;
using Oficina.Domain.OrderService;
using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.WorkshopServices;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Tests.Infrastructure;

public sealed class ServiceOrderRepositoryTests
{
    [Fact]
    public async Task AddAsync_and_ListAsync_should_persist_and_return_order_with_items()
    {
        await using var context = CreateContext();
        var customer = await AddCustomerAsync(context);
        var repository = new ServiceOrderRepository(context);
        var serviceOrder = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "Revisao");

        await repository.AddAsync(serviceOrder, CancellationToken.None);
        var result = await repository.ListAsync(CancellationToken.None);

        Assert.Equal(serviceOrder.Id, Assert.Single(result).Id);
    }

    [Fact]
    public async Task GetByIdAsync_should_return_null_when_order_does_not_exist()
    {
        await using var context = CreateContext();
        var repository = new ServiceOrderRepository(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ListSchedulesAsync_should_return_orders_scheduled_within_the_next_30_days()
    {
        await using var context = CreateContext();
        var customer = await AddCustomerAsync(context);
        var repository = new ServiceOrderRepository(context);
        var serviceOrder = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "Revisao");
        await repository.AddAsync(serviceOrder, CancellationToken.None);

        var result = await repository.ListSchedulesAsync(CancellationToken.None);

        Assert.Equal(serviceOrder.Id, Assert.Single(result).Id);
    }

    [Fact]
    public async Task ListSchedulesByDateAsync_should_filter_orders_by_date()
    {
        await using var context = CreateContext();
        var customer = await AddCustomerAsync(context);
        var repository = new ServiceOrderRepository(context);
        var serviceOrder = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "Revisao");
        await repository.AddAsync(serviceOrder, CancellationToken.None);

        var matching = await repository.ListSchedulesByDateAsync(DateTimeOffset.UtcNow, CancellationToken.None);
        var notMatching = await repository.ListSchedulesByDateAsync(DateTimeOffset.UtcNow.AddDays(-5), CancellationToken.None);

        Assert.Single(matching);
        Assert.Empty(notMatching);
    }

    [Fact]
    public async Task UpdateAsync_should_persist_new_parts_and_workshop_services()
    {
        await using var context = CreateContext();
        var customer = await AddCustomerAsync(context);
        var part = await AddPartAsync(context);
        var workshopService = await AddWorkshopServiceAsync(context);
        var repository = new ServiceOrderRepository(context);
        var serviceOrder = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "Revisao");
        await repository.AddAsync(serviceOrder, CancellationToken.None);

        var newPart = ServiceOrderPart.Create(part.Id, serviceOrder.Id, 2);
        var newWorkshopService = ServiceOrderWorkshop.Create(serviceOrder.Id, workshopService.Id);
        serviceOrder.Update(
            mechanicId: null, description: null, checkList: "Checklist ok",
            parts: [newPart], workshopServices: [newWorkshopService]);

        await repository.UpdateAsync(serviceOrder, [newPart], [newWorkshopService], CancellationToken.None);

        var result = await repository.GetByIdAsync(serviceOrder.Id, CancellationToken.None);
        Assert.Single(result!.Parts);
        Assert.Single(result.WorkshopServices);
    }

    private static async Task<Customer> AddCustomerAsync(AppDbContext context)
    {
        var customer = Customer.Create("Ana Silva", "ana@email.com", "11999990000", "11144477735");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    private static async Task<Part> AddPartAsync(AppDbContext context)
    {
        var part = Part.Create("Filtro", "COD-001", 10m, EnumPartKind.Part);
        context.Parts.Add(part);
        await context.SaveChangesAsync();
        return part;
    }

    private static async Task<WorkshopService> AddWorkshopServiceAsync(AppDbContext context)
    {
        var service = WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30);
        context.WorkshopServices.Add(service);
        await context.SaveChangesAsync();
        return service;
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"oficina-serviceorders-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }
}

using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Customers;
using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.WorkshopServices;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Tests.Infrastructure;

public sealed class ServiceOrderWithItemsPersistenceTests
{
    [Fact]
    public async Task AddAsync_should_persist_order_opened_with_items_without_reinserting_catalog_entities()
    {
        var databaseName = $"oficina-serviceorders-with-items-{Guid.NewGuid()}";
        Guid serviceOrderId;

        await using (var context = CreateContext(databaseName))
        {
            var customer = Customer.Create("Ana Silva", "ana@email.com", "11999990000", "11144477735");
            context.Customers.Add(customer);
            context.Parts.Add(Part.Create("Filtro", "COD-001", 10m, EnumPartKind.Part));
            context.WorkshopServices.Add(WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30));
            await context.SaveChangesAsync();

            var part = await new PartRepository(context).GetByIdAsync(context.Parts.Single().Id, CancellationToken.None);
            var workshopService = await new WorkshopServiceRepository(context)
                .GetByIdAsync(context.WorkshopServices.Single().Id, CancellationToken.None);

            var serviceOrder = ServiceOrder.Open(customer.Id, Guid.NewGuid(), "Revisao completa");
            var serviceOrderWorkshop = ServiceOrderWorkshop.Create(serviceOrder.Id, workshopService!.Id);
            serviceOrderWorkshop.ServiceOrder = serviceOrder;
            serviceOrderWorkshop.WorkshopService = workshopService;
            serviceOrder.AddWorkshopService(serviceOrderWorkshop);
            var serviceOrderPart = ServiceOrderPart.Create(part!.Id, serviceOrder.Id, 2);
            serviceOrderPart.OrderService = serviceOrder;
            serviceOrderPart.Part = part;
            serviceOrder.AddPart(serviceOrderPart);
            serviceOrder.RequestApprovalOnOpening();

            await new ServiceOrderRepository(context).AddAsync(serviceOrder, CancellationToken.None);
            serviceOrderId = serviceOrder.Id;
        }

        await using (var context = CreateContext(databaseName))
        {
            var result = await new ServiceOrderRepository(context).GetByIdAsync(serviceOrderId, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(ServiceOrderStatus.AwaitingApproval, result.Status);
            Assert.Equal(2, Assert.Single(result.Parts).QuantityUsed);
            Assert.Single(result.WorkshopServices);
            Assert.Equal(20m, result.TotalParts);
            Assert.Equal(1, await context.Parts.CountAsync());
            Assert.Equal(1, await context.WorkshopServices.CountAsync());
        }
    }

    private static AppDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new AppDbContext(options);
    }
}

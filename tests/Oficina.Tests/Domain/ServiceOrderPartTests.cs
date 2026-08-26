using Oficina.Domain.OrderService;

namespace Oficina.Tests.Domain;

public sealed class ServiceOrderPartTests
{
    [Fact]
    public void Create_should_reject_non_positive_quantity()
    {
        var act = () => ServiceOrderPart.Create(Guid.NewGuid(), Guid.NewGuid(), 0);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Create_should_set_ids_and_quantity()
    {
        var partId = Guid.NewGuid();
        var orderServiceId = Guid.NewGuid();

        var serviceOrderPart = ServiceOrderPart.Create(partId, orderServiceId, 3);

        Assert.Equal(partId, serviceOrderPart.PartId);
        Assert.Equal(orderServiceId, serviceOrderPart.OrderServiceId);
        Assert.Equal(3, serviceOrderPart.QuantityUsed);
    }

    [Fact]
    public void UpdateQuantity_should_change_quantity_used()
    {
        var serviceOrderPart = ServiceOrderPart.Create(Guid.NewGuid(), Guid.NewGuid(), 3);

        serviceOrderPart.UpdateQuantity(7);

        Assert.Equal(7, serviceOrderPart.QuantityUsed);
    }

    [Fact]
    public void UpdateQuantity_should_reject_non_positive_quantity()
    {
        var serviceOrderPart = ServiceOrderPart.Create(Guid.NewGuid(), Guid.NewGuid(), 3);

        var act = () => serviceOrderPart.UpdateQuantity(0);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }
}

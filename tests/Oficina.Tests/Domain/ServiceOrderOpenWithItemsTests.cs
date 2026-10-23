using Oficina.Domain.ServiceOrders;

namespace Oficina.Tests.Domain;

public sealed class ServiceOrderOpenWithItemsTests
{
    [Fact]
    public void RequestApprovalOnOpening_moves_created_order_with_services_to_AwaitingApproval()
    {
        var serviceOrder = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Revisao completa");
        serviceOrder.AddWorkshopService(ServiceOrderWorkshop.Create(serviceOrder.Id, Guid.NewGuid()));

        var path = serviceOrder.RequestApprovalOnOpening();

        Assert.Equal(ServiceOrderStatus.AwaitingApproval, serviceOrder.Status);
        Assert.Equal(
            [ServiceOrderStatus.Received, ServiceOrderStatus.InDiagnosis, ServiceOrderStatus.AwaitingApproval],
            path);
        Assert.Null(serviceOrder.MechanicId);
        Assert.Null(serviceOrder.CheckList);
    }

    [Fact]
    public void RequestApprovalOnOpening_rejects_order_without_workshop_services()
    {
        var serviceOrder = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Revisao completa");

        Assert.Throws<InvalidOperationException>(() => serviceOrder.RequestApprovalOnOpening());
        Assert.Equal(ServiceOrderStatus.Created, serviceOrder.Status);
    }

    [Fact]
    public void RequestApprovalOnOpening_rejects_order_that_is_not_in_Created_status()
    {
        var serviceOrder = ServiceOrder.Open(Guid.NewGuid(), Guid.NewGuid(), "Revisao completa");
        serviceOrder.AddWorkshopService(ServiceOrderWorkshop.Create(serviceOrder.Id, Guid.NewGuid()));
        serviceOrder.RequestApprovalOnOpening();

        Assert.Throws<InvalidOperationException>(() => serviceOrder.RequestApprovalOnOpening());
        Assert.Equal(ServiceOrderStatus.AwaitingApproval, serviceOrder.Status);
    }
}

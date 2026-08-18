using Oficina.Domain.ServiceOrders;

namespace Oficina.Tests.Domain;

public sealed class ServiceOrderTests
{
    [Fact]
    public void Update_preserves_optional_text_when_it_is_not_provided()
    {
        var serviceOrder = ServiceOrder.Open(Guid.NewGuid(), "Initial description");

        serviceOrder.Update(
            mechanicId: null,
            description: null,
            checkList: null,
            parts: null,
            workshopServices: null);

        Assert.Equal("Initial description", serviceOrder.Description);
        Assert.Null(serviceOrder.CheckList);
    }
}

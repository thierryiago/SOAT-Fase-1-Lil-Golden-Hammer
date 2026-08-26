using Oficina.Domain.OrderServiceHistory;

namespace Oficina.Tests.Domain;

public sealed class ServiceOrderHistoryTests
{
    [Fact]
    public void Create_should_reject_empty_order_service_id()
    {
        var act = () => ServiceOrderHistory.Create(Guid.Empty, "Received");

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_should_trim_status_name()
    {
        var orderServiceId = Guid.NewGuid();

        var history = ServiceOrderHistory.Create(orderServiceId, "  Received  ");

        Assert.Equal(orderServiceId, history.OrderServiceId);
        Assert.Equal("Received", history.StatusName);
    }

    [Fact]
    public void Create_should_default_status_name_to_unknown_when_null()
    {
        var history = ServiceOrderHistory.Create(Guid.NewGuid(), null);

        Assert.Equal("Unknown", history.StatusName);
    }

    [Fact]
    public void Create_should_default_status_name_to_unknown_when_blank()
    {
        var history = ServiceOrderHistory.Create(Guid.NewGuid(), "   ");

        Assert.Equal("Unknown", history.StatusName);
    }
}

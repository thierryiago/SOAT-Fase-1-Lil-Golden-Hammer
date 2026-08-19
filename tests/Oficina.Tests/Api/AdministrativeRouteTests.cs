using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Controllers;

namespace Oficina.Tests.Api;

public sealed class AdministrativeRouteTests
{
    public static TheoryData<Type, string> VersionedControllers => new()
    {
        { typeof(CustomersController), "api/v1/customers" },
        { typeof(VehiclesController), "api/v1/vehicles" },
        { typeof(MechanicsController), "api/v1/mechanics" },
        { typeof(ServicesController), "api/v1/services" },
        { typeof(PartsController), "api/v1/parts" },
        { typeof(StocksController), "api/v1/stocks" },
        { typeof(ServiceOrdersController), "api/v1/service-orders" },
        { typeof(ServiceOrderHistoryController), "api/v1/service-order-history" }
    };

    [Theory]
    [MemberData(nameof(VersionedControllers))]
    public void Administrative_controller_should_use_v1_prefix(Type controllerType, string expectedTemplate)
    {
        var route = Assert.Single(controllerType.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>());

        Assert.Equal(expectedTemplate, route.Template);
    }
}

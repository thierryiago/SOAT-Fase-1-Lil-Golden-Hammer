using Microsoft.AspNetCore.Authorization;
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
        { typeof(MetricsController), "api/v1/metrics" },
        { typeof(WorkshopServicesController), "api/v1/workshop-services" },
        { typeof(PartsController), "api/v1/parts" },
        { typeof(StocksController), "api/v1/stocks" },
        { typeof(ServiceOrdersController), "api/v1/service-orders" },
        { typeof(ServiceOrderHistoryController), "api/v1/service-order-history" },
        { typeof(ScheduleController), "api/v1/schedules" }
    };

    [Theory]
    [MemberData(nameof(VersionedControllers))]
    public void Administrative_controller_should_use_v1_prefix(Type controllerType, string expectedTemplate)
    {
        var route = Assert.Single(controllerType.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>());

        Assert.Equal(expectedTemplate, route.Template);
    }

    [Theory]
    [MemberData(nameof(VersionedControllers))]
    public void Administrative_controller_should_require_authorization(Type controllerType, string _)
    {
        Assert.NotEmpty(controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), true));
        Assert.Empty(controllerType.GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
    }
}

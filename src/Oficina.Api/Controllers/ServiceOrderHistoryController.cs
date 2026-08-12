using Microsoft.AspNetCore.Mvc;
using Oficina.Application.OrderServiceHistory;
using Oficina.Domain.OrderServiceHistory;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/service-order-history")]
public sealed class ServiceOrderHistoryController : ControllerBase
{
    private readonly ServiceOrderHistoryService _serviceOrderHistory;

    public ServiceOrderHistoryController(ServiceOrderHistoryService serviceOrderHistory)
    {
        _serviceOrderHistory = serviceOrderHistory;
    }

    [HttpGet(Name = "FindAllServiceOrderHistory")]
    [ProducesResponseType(typeof(List<ServiceOrderHistory>), StatusCodes.Status200OK)]
    public async Task<IActionResult> FindAll(CancellationToken cancellationToken)
    {
        var history = await _serviceOrderHistory.FindAllAsync(cancellationToken);
        return Ok(history);
    }

    [HttpGet("service-order/{serviceOrderId:guid}", Name = "FindServiceOrderHistoryByServiceOrder")]
    [ProducesResponseType(typeof(List<ServiceOrderHistory>), StatusCodes.Status200OK)]
    public async Task<IActionResult> FindByServiceOrder(Guid serviceOrderId, CancellationToken cancellationToken)
    {
        var history = await _serviceOrderHistory.FindByServiceOrderAsync(serviceOrderId, cancellationToken);
        return Ok(history);
    }
}

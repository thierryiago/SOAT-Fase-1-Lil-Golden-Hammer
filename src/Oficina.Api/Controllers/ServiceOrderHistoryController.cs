using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.OrderServiceHistory;
using Oficina.Application.OrderServiceHistory.UseCases.Queries;
using System.Diagnostics.CodeAnalysis;

namespace Oficina.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/service-order-history")]
[ExcludeFromCodeCoverage]
public sealed class ServiceOrderHistoryController : ControllerBase
{
    private readonly ListServiceOrderHistoryUseCase _listServiceOrderHistory;
    private readonly GetServiceOrderHistoryByServiceOrderUseCase _getServiceOrderHistoryByServiceOrder;

    public ServiceOrderHistoryController(
        ListServiceOrderHistoryUseCase listServiceOrderHistory,
        GetServiceOrderHistoryByServiceOrderUseCase getServiceOrderHistoryByServiceOrder)
    {
        _listServiceOrderHistory = listServiceOrderHistory;
        _getServiceOrderHistoryByServiceOrder = getServiceOrderHistoryByServiceOrder;
    }

    [HttpGet(Name = "FindAllServiceOrderHistory")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ServiceOrderHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> FindAll(CancellationToken cancellationToken)
    {
        var history = await _listServiceOrderHistory.FindAllAsync(cancellationToken);
        return Ok(history);
    }

    [HttpGet("service-order/{serviceOrderId:guid}", Name = "FindServiceOrderHistoryByServiceOrder")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ServiceOrderHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> FindByServiceOrder(Guid serviceOrderId, CancellationToken cancellationToken)
    {
        var history = await _getServiceOrderHistoryByServiceOrder.FindByServiceOrderAsync(serviceOrderId, cancellationToken);
        return Ok(history);
    }
}

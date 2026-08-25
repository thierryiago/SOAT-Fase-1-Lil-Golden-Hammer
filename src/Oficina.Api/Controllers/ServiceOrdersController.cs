using Microsoft.AspNetCore.Mvc;
using Oficina.Application.ServiceOrders;

namespace Oficina.Api.Controllers;

[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
[Route("api/v1/service-orders")]
public sealed class ServiceOrdersController : ControllerBase
{
    private readonly ServiceOrderService _serviceOrders;

    public ServiceOrdersController(ServiceOrderService serviceOrders)
    {
        _serviceOrders = serviceOrders;
    }

    [HttpGet(Name = "ListServiceOrders")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ServiceOrderListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var serviceOrders = await _serviceOrders.ListAsync(cancellationToken);
        return Ok(serviceOrders);
    }

    [HttpGet("{id:guid}", Name = "GetServiceOrderById")]
    [ProducesResponseType(typeof(ServiceOrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var serviceOrder = await _serviceOrders.GetByIdAsync(id, cancellationToken);
        return serviceOrder is null ? NotFound() : Ok(serviceOrder);
    }

    [HttpPost(Name = "OpenServiceOrder")]
    [ProducesResponseType(typeof(ServiceOrderDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Open(OpenServiceOrderRequest request, CancellationToken cancellationToken)
    {
        var serviceOrder = await _serviceOrders.OpenAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = serviceOrder.Id }, serviceOrder);
    }

    [HttpPut(Name = "UpdateServiceOrder")]
    [ProducesResponseType(typeof(ServiceOrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(UpdateServiceOrderRequest request, CancellationToken cancellationToken)
    {
        var service = await _serviceOrders.UpdateAsync(request, cancellationToken);
        return Ok(service);
    }

    [HttpPost("{id:guid}/approve", Name = "ApproveServiceOrder")]
    [ProducesResponseType(typeof(ServiceOrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var service = await _serviceOrders.ApproveAsync(id, cancellationToken);
        return Ok(service);
    }

    [HttpPost("{id:guid}/cancel", Name = "CancelServiceOrder")]
    [ProducesResponseType(typeof(ServiceOrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var service = await _serviceOrders.CancelAsync(id, cancellationToken);
        return Ok(service);
    }

    [HttpPost("{id:guid}/finalize", Name = "FinalizeServiceOrder")]
    [ProducesResponseType(typeof(ServiceOrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Finalize(Guid id, CancellationToken cancellationToken)
    {
        var service = await _serviceOrders.FinalizeAsync(id, cancellationToken);
        return Ok(service);
    }

    [HttpPost("{id:guid}/deliver", Name = "DeliverServiceOrder")]
    [ProducesResponseType(typeof(ServiceOrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Deliver(Guid id, CancellationToken cancellationToken)
    {
        var service = await _serviceOrders.DeliverAsync(id, cancellationToken);
        return Ok(service);
    }

}

using Microsoft.AspNetCore.Mvc;
using Oficina.Application.ServiceOrders;
using Oficina.Application.Services;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/service-orders")]
public sealed class ServiceOrdersController : ControllerBase
{
    private readonly ServiceOrderService _serviceOrders;

    public ServiceOrdersController(ServiceOrderService serviceOrders)
    {
        _serviceOrders = serviceOrders;
    }

    [HttpGet(Name = "ListServiceOrders")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ServiceOrder>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var serviceOrders = await _serviceOrders.ListAsync(cancellationToken);
        return Ok(serviceOrders);
    }

    [HttpGet("{id:guid}", Name = "GetServiceOrderById")]
    [ProducesResponseType(typeof(ServiceOrder), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var serviceOrder = await _serviceOrders.GetByIdAsync(id, cancellationToken);
        return serviceOrder is null ? NotFound() : Ok(serviceOrder);
    }

    [HttpPost(Name = "OpenServiceOrder")]
    [ProducesResponseType(typeof(ServiceOrder), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Open(OpenServiceOrderRequest request, CancellationToken cancellationToken)
    {
        var serviceOrder = await _serviceOrders.OpenAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = serviceOrder.Id }, serviceOrder);
    }

    [HttpPut(Name = "UpdateServiceOrder")]
    [ProducesResponseType(typeof(ServiceOrder), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(UpdateServiceOrderRequest request, CancellationToken cancellationToken)
    {
        var service = await _serviceOrders.UpdateAsync(request, cancellationToken);
        return Ok(service);
    }

    [HttpPost("{id:guid}/parts", Name = "AddPartToServiceOrder")]
    [ProducesResponseType(typeof(ServiceOrder), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddPart(
        Guid id,
        AddPartToServiceOrderRequest request,
        CancellationToken cancellationToken)
    {
        var serviceOrder = await _serviceOrders.AddPartAsync(id, request, cancellationToken);
        return Ok(serviceOrder);
    }
}

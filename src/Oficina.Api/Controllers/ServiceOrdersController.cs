using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.ServiceOrders;
using Oficina.Application.ServiceOrders.UseCases;
using Oficina.Application.ServiceOrders.UseCases.Queries;
using System.Diagnostics.CodeAnalysis;

namespace Oficina.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/service-orders")]
[ExcludeFromCodeCoverage]
public sealed class ServiceOrdersController : ControllerBase
{
    private readonly ApproveServiceOrderUseCase _approveServiceOrder;
    private readonly CancelServiceOrderUseCase _cancelServiceOrder;
    private readonly FinalizeServiceOrderUseCase _finalizeServiceOrder;
    private readonly DeliverServiceOrderUseCase _deliverServiceOrder;
    private readonly ListServiceOrdersUseCase _listServiceOrders;
    private readonly GetServiceOrderByIdUseCase _getServiceOrderById;
    private readonly TrackServiceOrderUseCase _trackServiceOrder;
    private readonly TrackServiceOrdersByDocumentUseCase _trackServiceOrdersByDocument;
    private readonly OpenServiceOrderUseCase _openServiceOrder;
    private readonly UpdateServiceOrderUseCase _updateServiceOrder;

    public ServiceOrdersController(
        ApproveServiceOrderUseCase approveServiceOrder,
        CancelServiceOrderUseCase cancelServiceOrder,
        FinalizeServiceOrderUseCase finalizeServiceOrder,
        DeliverServiceOrderUseCase deliverServiceOrder,
        OpenServiceOrderUseCase openServiceOrder,
        UpdateServiceOrderUseCase updateServiceOrder,
        ListServiceOrdersUseCase listServiceOrders,
        GetServiceOrderByIdUseCase getServiceOrderById,
        TrackServiceOrderUseCase trackServiceOrder,
        TrackServiceOrdersByDocumentUseCase trackServiceOrdersByDocument)
    {
        _approveServiceOrder = approveServiceOrder;
        _cancelServiceOrder = cancelServiceOrder;
        _finalizeServiceOrder = finalizeServiceOrder;
        _deliverServiceOrder = deliverServiceOrder;
        _openServiceOrder = openServiceOrder;
        _updateServiceOrder = updateServiceOrder;
        _listServiceOrders = listServiceOrders;
        _getServiceOrderById = getServiceOrderById;
        _trackServiceOrder = trackServiceOrder;
        _trackServiceOrdersByDocument = trackServiceOrdersByDocument;
    }

    [HttpGet(Name = "ListServiceOrders")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ServiceOrderListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var serviceOrders = await _listServiceOrders.ExecuteAsync(cancellationToken);
        return Ok(serviceOrders);
    }

    [HttpGet("{id:guid}", Name = "GetServiceOrderById")]
    [ProducesResponseType(typeof(ServiceOrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var serviceOrder = await _getServiceOrderById.ExecuteAsync(id, cancellationToken);
        return serviceOrder is null ? NotFound() : Ok(serviceOrder);
    }

    [HttpGet("{id:guid}/track", Name = "TrackServiceOrder")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServiceOrderTrackingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Track(Guid id, [FromQuery] string document, CancellationToken cancellationToken)
    {
        var result = await _trackServiceOrder.ExecuteAsync(id, document, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("track", Name = "TrackServiceOrdersByDocument")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyCollection<ServiceOrderTrackingSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TrackByDocument([FromQuery] string document, CancellationToken cancellationToken)
    {
        var result = await _trackServiceOrdersByDocument.ExecuteAsync(document, cancellationToken);
        return Ok(result);
    }

    [HttpPost(Name = "OpenServiceOrder")]
    [ProducesResponseType(typeof(ServiceOrderDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Open(OpenServiceOrderRequest request, CancellationToken cancellationToken)
    {
        var serviceOrder = await _openServiceOrder.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = serviceOrder.Id }, serviceOrder);
    }

    [HttpPut(Name = "UpdateServiceOrder")]
    [ProducesResponseType(typeof(ServiceOrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(UpdateServiceOrderRequest request, CancellationToken cancellationToken)
    {
        var service = await _updateServiceOrder.ExecuteAsync(request, cancellationToken);
        return Ok(service);
    }

    [HttpPost("{id:guid}/approve", Name = "ApproveServiceOrder")]
    [ProducesResponseType(typeof(ServiceOrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var service = await _approveServiceOrder.ExecuteAsync(id, cancellationToken);
        return Ok(service);
    }

    [HttpPost("{id:guid}/cancel", Name = "CancelServiceOrder")]
    [ProducesResponseType(typeof(ServiceOrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var service = await _cancelServiceOrder.ExecuteAsync(id, cancellationToken);
        return Ok(service);
    }

    [HttpPost("{id:guid}/finalize", Name = "FinalizeServiceOrder")]
    [ProducesResponseType(typeof(ServiceOrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Finalize(Guid id, CancellationToken cancellationToken)
    {
        var service = await _finalizeServiceOrder.ExecuteAsync(id, cancellationToken);
        return Ok(service);
    }

    [HttpPost("{id:guid}/deliver", Name = "DeliverServiceOrder")]
    [ProducesResponseType(typeof(ServiceOrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Deliver(Guid id, CancellationToken cancellationToken)
    {
        var service = await _deliverServiceOrder.ExecuteAsync(id, cancellationToken);
        return Ok(service);
    }

}

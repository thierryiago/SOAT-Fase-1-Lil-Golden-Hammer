using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Common;
using Oficina.Application.WorkshopServices;
using System.Diagnostics.CodeAnalysis;

namespace Oficina.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/workshop-services")]
[ExcludeFromCodeCoverage]
public sealed class WorkshopServicesController : ControllerBase
{
    private readonly ServiceCatalogService _services;

    public WorkshopServicesController(ServiceCatalogService services)
    {
        _services = services;
    }

    [HttpGet(Name = "ListWorkshopServices")]
    [ProducesResponseType(typeof(PagedResponse<WorkshopServiceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        var services = await _services.ListAsync(request, cancellationToken);
        return Ok(services);
    }

    [HttpGet("{id:guid}", Name = "GetWorkshopServiceById")]
    [ProducesResponseType(typeof(WorkshopServiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var service = await _services.GetByIdAsync(id, cancellationToken);
        return service is null ? NotFound() : Ok(service);
    }

    [HttpPost(Name = "CreateWorkshopService")]
    [ProducesResponseType(typeof(WorkshopServiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        CreateWorkshopServiceRequest request,
        CancellationToken cancellationToken)
    {
        var service = await _services.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = service.Id }, service);
    }

    [HttpPut("{id:guid}", Name = "UpdateWorkshopService")]
    [ProducesResponseType(typeof(WorkshopServiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateWorkshopServiceRequest request,
        CancellationToken cancellationToken)
    {
        var service = await _services.UpdateAsync(id, request, cancellationToken);
        return Ok(service);
    }

    [HttpDelete("{id:guid}", Name = "DeleteWorkshopService")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _services.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}

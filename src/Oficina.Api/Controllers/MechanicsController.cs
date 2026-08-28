using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Common;
using Oficina.Application.Mechanics;
using System.Diagnostics.CodeAnalysis;

namespace Oficina.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/mechanics")]
[ExcludeFromCodeCoverage]
public sealed class MechanicsController : ControllerBase
{
    private readonly MechanicService _mechanics;

    public MechanicsController(MechanicService mechanics)
    {
        _mechanics = mechanics;
    }

    [HttpGet(Name = "ListMechanics")]
    [ProducesResponseType(typeof(PagedResponse<MechanicResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        var mechanics = await _mechanics.ListAsync(request, cancellationToken);
        return Ok(mechanics);
    }

    [HttpGet("{id:guid}", Name = "GetMechanicById")]
    [ProducesResponseType(typeof(MechanicResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var mechanic = await _mechanics.GetByIdAsync(id, cancellationToken);
        return mechanic is null ? NotFound() : Ok(mechanic);
    }

    [HttpPost(Name = "CreateMechanic")]
    [ProducesResponseType(typeof(MechanicResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateMechanicRequest request, CancellationToken cancellationToken)
    {
        var mechanic = await _mechanics.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = mechanic.Id }, mechanic);
    }

    [HttpPut("{id:guid}", Name = "UpdateMechanic")]
    [ProducesResponseType(typeof(MechanicResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateMechanicRequest request,
        CancellationToken cancellationToken)
    {
        var mechanic = await _mechanics.UpdateAsync(id, request, cancellationToken);
        return Ok(mechanic);
    }

    [HttpDelete("{id:guid}", Name = "DeleteMechanic")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _mechanics.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}

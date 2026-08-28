using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Common;
using Oficina.Application.Parts;
using System.Diagnostics.CodeAnalysis;

namespace Oficina.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/parts")]
[ExcludeFromCodeCoverage]
public sealed class PartsController : ControllerBase
{
    private readonly PartService _parts;

    public PartsController(PartService parts)
    {
        _parts = parts;
    }

    [HttpGet(Name = "ListParts")]
    [ProducesResponseType(typeof(PagedResponse<PartResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        var parts = await _parts.ListAsync(request, cancellationToken);
        return Ok(parts);
    }

    [HttpGet("{id:guid}", Name = "GetPartById")]
    [ProducesResponseType(typeof(PartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var part = await _parts.GetByIdAsync(id, cancellationToken);
        return part is null ? NotFound() : Ok(part);
    }

    [HttpPost(Name = "CreatePart")]
    [ProducesResponseType(typeof(PartResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePartRequest request,
        CancellationToken cancellationToken)
    {
        var part = await _parts.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = part.Id }, part);
    }

    [HttpPut("{id:guid}", Name = "UpdatePart")]
    [ProducesResponseType(typeof(PartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePartRequest request,
        CancellationToken cancellationToken)
    {
        var part = await _parts.UpdateAsync(id, request, cancellationToken);
        return Ok(part);
    }

    [HttpDelete("{id:guid}", Name = "DeletePart")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _parts.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}

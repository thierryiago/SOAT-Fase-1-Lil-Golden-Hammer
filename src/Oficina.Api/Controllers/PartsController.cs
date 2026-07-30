using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Parts;
using Oficina.Domain.Parts;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/parts")]
public sealed class PartsController : ControllerBase
{
    private readonly PartService _parts;

    public PartsController(PartService parts)
    {
        _parts = parts;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<Part>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var parts = await _parts.ListAsync(cancellationToken);
        return Ok(parts);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Part), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var part = await _parts.GetByIdAsync(id, cancellationToken);
        return part is null ? NotFound() : Ok(part);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Part), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreatePartRequest request, CancellationToken cancellationToken)
    {
        var part = await _parts.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = part.Id }, part);
    }
}

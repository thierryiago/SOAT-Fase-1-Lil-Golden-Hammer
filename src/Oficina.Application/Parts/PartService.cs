using Oficina.Domain.Parts;

namespace Oficina.Application.Parts;

public sealed class PartService
{
    private readonly IPartRepository _parts;

    public PartService(IPartRepository parts)
    {
        _parts = parts;
    }

    public async Task<Part> GetActivePartAsync(Guid id, CancellationToken cancellationToken)
    {
        var part = await _parts.GetByIdAsync(id, cancellationToken);
        if (part is null || !part.IsActive)
        {
            throw new KeyNotFoundException("Part or consumable was not found.");
        }

        return part;
    }
}

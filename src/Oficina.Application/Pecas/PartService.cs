using Oficina.Domain.Parts;

namespace Oficina.Application.Parts;

public sealed class PartService
{
    private readonly IPartRepository _parts;

    public PartService(IPartRepository parts)
    {
        _parts = parts;
    }

    public Task<IReadOnlyCollection<Part>> ListAsync(CancellationToken cancellationToken) =>
        _parts.ListAsync(cancellationToken);

    public Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _parts.GetByIdAsync(id, cancellationToken);

    public async Task<Part> CreateAsync(CreatePartRequest request, CancellationToken cancellationToken)
    {
        var part = Part.Create(request.Name, request.Code, request.UnitPrice, request.StockQuantity);
        await _parts.AddAsync(part, cancellationToken);
        return part;
    }
}

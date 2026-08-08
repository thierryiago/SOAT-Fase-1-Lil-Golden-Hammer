using Oficina.Application.Common;
using Oficina.Domain.Parts;

namespace Oficina.Application.Parts;

public sealed class PartService
{
    private readonly IPartRepository _parts;

    public PartService(IPartRepository parts)
    {
        _parts = parts;
    }

    public async Task<PagedResponse<PartResponse>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var parts = await _parts.ListAsync(cancellationToken);
        var search = request.Search?.Trim();
        var query = parts
            .Where(part => part.IsActive)
            .Where(part =>
                string.IsNullOrWhiteSpace(search) ||
                part.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                part.Code.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(part => part.Name)
            .Select(Map);

        return Pagination.Create(query, request);
    }

    public async Task<PartResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var part = await _parts.GetByIdAsync(id, cancellationToken);
        return part is null || !part.IsActive ? null : Map(part);
    }

    public async Task<PartResponse> CreateAsync(
        CreatePartRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await _parts.GetByCodeAsync(request.Code, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("A part or consumable with the informed code already exists.");
        }

        var part = Part.Create(
            request.Name,
            request.Code,
            request.UnitPrice,
            request.Kind);
        await _parts.AddAsync(part, cancellationToken);
        return Map(part);
    }

    public async Task<PartResponse> UpdateAsync(
        Guid id,
        UpdatePartRequest request,
        CancellationToken cancellationToken)
    {
        var part = await GetActivePartAsync(id, cancellationToken);
        var codeOwner = await _parts.GetByCodeAsync(request.Code, cancellationToken);
        if (codeOwner is not null && codeOwner.Id != id)
        {
            throw new ConflictException("A part or consumable with the informed code already exists.");
        }

        part.Update(request.Name, request.Code, request.UnitPrice, request.Kind);
        await _parts.UpdateAsync(part, cancellationToken);
        return Map(part);
    }

    public async Task<PartResponse> AdjustStockAsync(
        Guid id,
        AdjustStockRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Stock adjustment reason is required.", nameof(request.Reason));
        }

        var part = await GetActivePartAsync(id, cancellationToken);
        // part.AdjustStock(request.Quantity);
        await _parts.UpdateAsync(part, cancellationToken);
        return Map(part);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var part = await _parts.GetByIdAsync(id, cancellationToken);
        if (part is null || !part.IsActive)
        {
            return false;
        }

        part.Deactivate();
        await _parts.UpdateAsync(part, cancellationToken);
        return true;
    }

    private async Task<Part> GetActivePartAsync(Guid id, CancellationToken cancellationToken)
    {
        var part = await _parts.GetByIdAsync(id, cancellationToken);
        if (part is null || !part.IsActive)
        {
            throw new KeyNotFoundException("Part or consumable was not found.");
        }

        return part;
    }

    private static PartResponse Map(Part part) =>
        new(
            part.Id,
            part.Name,
            part.Code,
            part.UnitPrice,
            part.Kind);
}

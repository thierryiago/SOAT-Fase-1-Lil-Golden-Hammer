using Oficina.Application.Common;
using Oficina.Domain.Mechanics;

namespace Oficina.Application.Mechanics;

public sealed class MechanicService
{
    private readonly IMechanicRepository _mechanics;

    public MechanicService(IMechanicRepository mechanics)
    {
        _mechanics = mechanics;
    }

    public async Task<PagedResponse<MechanicResponse>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var mechanics = await _mechanics.ListAsync(cancellationToken);
        var search = request.Search?.Trim();

        var query = mechanics
            .Where(mechanic => mechanic.IsActive)
            .Where(mechanic =>
                string.IsNullOrWhiteSpace(search) ||
                mechanic.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(mechanic => mechanic.Name)
            .Select(Map);

        return Pagination.Create(query, request);
    }

    public async Task<MechanicResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var mechanic = await _mechanics.GetByIdAsync(id, cancellationToken);
        return mechanic is null || !mechanic.IsActive ? null : Map(mechanic);
    }

    public async Task<MechanicResponse> CreateAsync(
        CreateMechanicRequest request,
        CancellationToken cancellationToken)
    {
        var mechanic = Mechanic.Create(request.Name);
        await _mechanics.AddAsync(mechanic, cancellationToken);
        return Map(mechanic);
    }

    public async Task<MechanicResponse> UpdateAsync(
        Guid id,
        UpdateMechanicRequest request,
        CancellationToken cancellationToken)
    {
        var mechanic = await GetActiveMechanicAsync(id, cancellationToken);
        mechanic.Update(request.Name);
        await _mechanics.UpdateAsync(mechanic, cancellationToken);
        return Map(mechanic);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var mechanic = await _mechanics.GetByIdAsync(id, cancellationToken);
        if (mechanic is null || !mechanic.IsActive)
        {
            return false;
        }

        mechanic.Deactivate();
        await _mechanics.UpdateAsync(mechanic, cancellationToken);
        return true;
    }

    private async Task<Mechanic> GetActiveMechanicAsync(Guid id, CancellationToken cancellationToken)
    {
        var mechanic = await _mechanics.GetByIdAsync(id, cancellationToken);
        if (mechanic is null || !mechanic.IsActive)
        {
            throw new KeyNotFoundException("Mechanic was not found.");
        }

        return mechanic;
    }

    private static MechanicResponse Map(Mechanic mechanic) =>
        new(mechanic.Id, mechanic.Name);
}

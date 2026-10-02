using Oficina.Domain.Mechanics;

namespace Oficina.Application.Mechanics.UseCases;

public class UpdateMechanicUseCase(
    IMechanicRepository mechanics)
{
    private readonly IMechanicRepository _mechanics = mechanics;

    public async Task<MechanicResponse> UpdateAsync(
        Guid id,
        UpdateMechanicRequest request,
        CancellationToken cancellationToken)
    {
        var mechanic = await GetActiveMechanicAsync(id, cancellationToken);
        mechanic.Update(request.Name);
        await _mechanics.UpdateAsync(mechanic, cancellationToken);
        return MechanicResponseMapper.Map(mechanic);
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
}

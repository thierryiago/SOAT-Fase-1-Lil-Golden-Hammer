namespace Oficina.Application.Mechanics.UseCases;

public class DeleteMechanicUseCase(
    IMechanicRepository mechanics)
{
    private readonly IMechanicRepository _mechanics = mechanics;

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
}

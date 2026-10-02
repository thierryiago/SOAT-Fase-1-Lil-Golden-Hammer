namespace Oficina.Application.Mechanics.UseCases.Queries;

public class GetMechanicByIdUseCase(
    IMechanicRepository mechanics)
{
    private readonly IMechanicRepository _mechanics = mechanics;

    public async Task<MechanicResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var mechanic = await _mechanics.GetByIdAsync(id, cancellationToken);
        return mechanic is null || !mechanic.IsActive ? null : MechanicResponseMapper.Map(mechanic);
    }
}

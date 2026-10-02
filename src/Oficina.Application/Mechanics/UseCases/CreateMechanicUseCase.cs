using Oficina.Domain.Mechanics;

namespace Oficina.Application.Mechanics.UseCases;

public class CreateMechanicUseCase(
    IMechanicRepository mechanics)
{
    private readonly IMechanicRepository _mechanics = mechanics;

    public async Task<MechanicResponse> CreateAsync(
        CreateMechanicRequest request,
        CancellationToken cancellationToken)
    {
        var mechanic = Mechanic.Create(request.Name);
        await _mechanics.AddAsync(mechanic, cancellationToken);
        return MechanicResponseMapper.Map(mechanic);
    }
}

using Oficina.Application.Common;

namespace Oficina.Application.Mechanics.UseCases.Queries;

public class ListMechanicsUseCase(
    IMechanicRepository mechanics)
{
    private readonly IMechanicRepository _mechanics = mechanics;

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
            .Select(MechanicResponseMapper.Map);

        return Pagination.Create(query, request);
    }
}

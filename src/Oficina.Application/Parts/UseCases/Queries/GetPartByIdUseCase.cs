namespace Oficina.Application.Parts.UseCases.Queries;

public class GetPartByIdUseCase(
    IPartRepository parts)
{
    private readonly IPartRepository _parts = parts;

    public async Task<PartResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var part = await _parts.GetByIdAsync(id, cancellationToken);
        return part is null || !part.IsActive ? null : PartResponseMapper.Map(part);
    }
}

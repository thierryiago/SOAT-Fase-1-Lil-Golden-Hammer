using Oficina.Application.Common;

namespace Oficina.Application.Parts.UseCases;

public class UpdatePartUseCase(
    IPartRepository parts,
    PartService partService)
{
    private readonly IPartRepository _parts = parts;
    private readonly PartService _partService = partService;

    public async Task<PartResponse> UpdateAsync(
        Guid id,
        UpdatePartRequest request,
        CancellationToken cancellationToken)
    {
        var part = await _partService.GetActivePartAsync(id, cancellationToken);
        var codeOwner = await _parts.GetByCodeAsync(request.Code, cancellationToken);
        if (codeOwner is not null && codeOwner.Id != id)
        {
            throw new ConflictException("A part or consumable with the informed code already exists.");
        }

        part.Update(request.Name, request.Code, request.UnitPrice, request.Kind);
        await _parts.UpdateAsync(part, cancellationToken);
        return PartResponseMapper.Map(part);
    }
}

namespace Oficina.Application.Parts.UseCases;

public class DeletePartUseCase(
    IPartRepository parts)
{
    private readonly IPartRepository _parts = parts;

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
}

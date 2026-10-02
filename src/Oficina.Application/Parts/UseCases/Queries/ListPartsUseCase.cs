using Oficina.Application.Common;

namespace Oficina.Application.Parts.UseCases.Queries;

public class ListPartsUseCase(
    IPartRepository parts)
{
    private readonly IPartRepository _parts = parts;

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
            .Select(PartResponseMapper.Map);

        return Pagination.Create(query, request);
    }
}

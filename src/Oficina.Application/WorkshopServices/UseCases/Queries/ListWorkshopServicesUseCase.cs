using Oficina.Application.Common;

namespace Oficina.Application.WorkshopServices.UseCases.Queries;

public class ListWorkshopServicesUseCase(
    IWorkshopServiceRepository services)
{
    private readonly IWorkshopServiceRepository _services = services;

    public async Task<PagedResponse<WorkshopServiceResponse>> ListAsync(
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var services = await _services.ListAsync(cancellationToken);
        var search = request.Search?.Trim();
        var query = services
            .Where(service => service.IsActive)
            .Where(service =>
                string.IsNullOrWhiteSpace(search) ||
                service.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                service.Description.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(service => service.Name)
            .Select(WorkshopServiceResponseMapper.Map);

        return Pagination.Create(query, request);
    }
}

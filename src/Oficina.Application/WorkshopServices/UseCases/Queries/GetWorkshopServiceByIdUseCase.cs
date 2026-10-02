namespace Oficina.Application.WorkshopServices.UseCases.Queries;

public class GetWorkshopServiceByIdUseCase(
    IWorkshopServiceRepository services)
{
    private readonly IWorkshopServiceRepository _services = services;

    public async Task<WorkshopServiceResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var service = await _services.GetByIdAsync(id, cancellationToken);
        return service is null || !service.IsActive ? null : WorkshopServiceResponseMapper.Map(service);
    }
}

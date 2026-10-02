using Oficina.Application.Common;
using Oficina.Domain.WorkshopServices;

namespace Oficina.Application.WorkshopServices.UseCases;

public class CreateWorkshopServiceUseCase(
    IWorkshopServiceRepository services)
{
    private readonly IWorkshopServiceRepository _services = services;

    public async Task<WorkshopServiceResponse> CreateAsync(
        CreateWorkshopServiceRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await _services.GetByNameAsync(request.Name, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("A workshop service with the informed name already exists.");
        }

        var service = WorkshopService.Create(
            request.Name,
            request.Description,
            request.UnitPrice,
            request.EstimatedDurationMinutes);
        await _services.AddAsync(service, cancellationToken);
        return WorkshopServiceResponseMapper.Map(service);
    }
}

using Oficina.Application.Common;
using Oficina.Domain.WorkshopServices;

namespace Oficina.Application.WorkshopServices.UseCases;

public class UpdateWorkshopServiceUseCase(
    IWorkshopServiceRepository services)
{
    private readonly IWorkshopServiceRepository _services = services;

    public async Task<WorkshopServiceResponse> UpdateAsync(
        Guid id,
        UpdateWorkshopServiceRequest request,
        CancellationToken cancellationToken)
    {
        var service = await GetActiveServiceAsync(id, cancellationToken);
        var nameOwner = await _services.GetByNameAsync(request.Name, cancellationToken);
        if (nameOwner is not null && nameOwner.Id != id)
        {
            throw new ConflictException("A workshop service with the informed name already exists.");
        }

        service.Update(
            request.Name,
            request.Description,
            request.UnitPrice,
            request.EstimatedDurationMinutes);
        await _services.UpdateAsync(service, cancellationToken);
        return WorkshopServiceResponseMapper.Map(service);
    }

    private async Task<WorkshopService> GetActiveServiceAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var service = await _services.GetByIdAsync(id, cancellationToken);
        if (service is null || !service.IsActive)
        {
            throw new KeyNotFoundException("Workshop service was not found.");
        }

        return service;
    }
}

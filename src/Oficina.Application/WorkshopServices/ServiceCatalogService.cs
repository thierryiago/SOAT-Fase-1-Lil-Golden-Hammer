using Oficina.Application.Common;
using Oficina.Domain.Services;

namespace Oficina.Application.WorkshopServices;

public sealed class ServiceCatalogService(IWorkshopServiceRepository services)
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
            .Select(Map);

        return Pagination.Create(query, request);
    }

    public async Task<WorkshopServiceResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var service = await _services.GetByIdAsync(id, cancellationToken);
        return service is null || !service.IsActive ? null : Map(service);
    }

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
        return Map(service);
    }

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
        return Map(service);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var service = await _services.GetByIdAsync(id, cancellationToken);
        if (service is null || !service.IsActive)
        {
            return false;
        }

        service.Deactivate();
        await _services.UpdateAsync(service, cancellationToken);
        return true;
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

    private static WorkshopServiceResponse Map(WorkshopService service) =>
        new(
            service.Id,
            service.Name,
            service.Description,
            service.UnitPrice,
            service.EstimatedDurationMinutes);
}

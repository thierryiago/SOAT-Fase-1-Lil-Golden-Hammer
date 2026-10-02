namespace Oficina.Application.WorkshopServices.UseCases;

public class DeleteWorkshopServiceUseCase(
    IWorkshopServiceRepository services)
{
    private readonly IWorkshopServiceRepository _services = services;

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
}

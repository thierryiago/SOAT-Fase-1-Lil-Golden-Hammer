using Oficina.Application.Common;
using Oficina.Application.WorkshopServices;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Tests.Application;

public sealed class ServiceCatalogServiceTests
{
    /*
    [Fact]
    public async Task Workshop_service_crud_should_manage_catalog_item()
    {
        var service = new ServiceCatalogService(new InMemoryWorkshopServiceRepository());
        var created = await service.CreateAsync(
            new CreateWorkshopServiceRequest("Troca de oleo", "Troca completa", 120m, 45),
            CancellationToken.None);

        var updated = await service.UpdateAsync(
            created.Id,
            new UpdateWorkshopServiceRequest("Troca de oleo premium", "Oleo e filtro", 180m, 60),
            CancellationToken.None);
        var page = await service.ListAsync(new PageRequest("premium", 1, 20), CancellationToken.None);

        Assert.Equal(180m, updated.UnitPrice);
        Assert.Equal(60, updated.EstimatedDurationMinutes);
        Assert.Single(page.Items);
        Assert.True(await service.DeleteAsync(created.Id, CancellationToken.None));
        Assert.Null(await service.GetByIdAsync(created.Id, CancellationToken.None));
    }
    */
}

using Oficina.Application.Common;
using Oficina.Application.Parts;
using Oficina.Domain.Parts;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Tests.Application;

public sealed class PartServiceTests
{
    /*
    [Fact]
    public async Task Part_crud_should_distinguish_parts_and_consumables()
    {
        var service = new PartService(new InMemoryPartRepository());
        var created = await service.CreateAsync(
            new CreatePartRequest("Oleo 5W30", "INS-001", 45m, EnumPartKind.Consumable),
            CancellationToken.None);

        var updated = await service.UpdateAsync(
            created.Id,
            new UpdatePartRequest("Oleo sintetico 5W30", "INS-001", 52m, EnumPartKind.Consumable),
            CancellationToken.None);
        var adjusted = await service.AdjustStockAsync(
            created.Id,
            new AdjustStockRequest(5, "Compra"),
            CancellationToken.None);
        var page = await service.ListAsync(new PageRequest("sintetico", 1, 20), CancellationToken.None);

        Assert.Equal(EnumPartKind.Consumable, updated.Kind);
        Assert.Equal(updated.Id, adjusted.Id);
        Assert.Equal(52m, adjusted.UnitPrice);
        Assert.Single(page.Items);
        Assert.True(await service.DeleteAsync(created.Id, CancellationToken.None));
        Assert.Null(await service.GetByIdAsync(created.Id, CancellationToken.None));
    }
    */
}

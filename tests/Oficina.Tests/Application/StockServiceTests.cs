using Oficina.Application.Common;
using Oficina.Application.Parts;
using Oficina.Application.Stocks;
using Oficina.Domain.Parts;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Tests.Application;

public sealed class StockServiceTests
{
    [Fact]
    public async Task CreateAsync_should_throw_conflict_when_stock_for_part_already_exists()
    {
        var partRepository = new InMemoryPartRepository();
        var part = Part.Create("Filtro", "FLT-001", 35.5m, 0);
        await partRepository.AddAsync(part, CancellationToken.None);

        var service = new StockService(new InMemoryStockRepository(), partRepository);

        await service.CreateAsync(new CreateStockRequest(part.Id, 10), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CreateStockRequest(part.Id, 5), CancellationToken.None));
    }

    [Fact]
    public async Task EntryAsync_should_create_stock_when_it_does_not_exist()
    {
        var partRepository = new InMemoryPartRepository();
        var part = Part.Create("Filtro", "FLT-002", 35.5m, 0);
        await partRepository.AddAsync(part, CancellationToken.None);

        var service = new StockService(new InMemoryStockRepository(), partRepository);

        var stock = await service.EntryAsync(part.Id, new StockMovementRequest(7), CancellationToken.None);

        Assert.Equal(7, stock.Quantity);
    }
}

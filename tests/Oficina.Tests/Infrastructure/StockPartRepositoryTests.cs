using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Stock;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Tests.Infrastructure;

public sealed class StockPartRepositoryTests
{
    [Fact]
    public async Task AddAsync_and_ListAsync_should_persist_and_return_stock_ordered_by_part()
    {
        await using var context = CreateContext();
        var repository = new StockPartRepository(context);
        var stock = StockPart.Create(Guid.NewGuid(), 10);

        await repository.AddAsync(stock, CancellationToken.None);
        var result = await repository.ListAsync(CancellationToken.None);

        Assert.Equal(stock.Id, Assert.Single(result).Id);
    }

    [Fact]
    public async Task GetByIdAsync_should_return_null_when_stock_does_not_exist()
    {
        await using var context = CreateContext();
        var repository = new StockPartRepository(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPartIdAsync_should_find_stock_by_part_id()
    {
        await using var context = CreateContext();
        var repository = new StockPartRepository(context);
        var partId = Guid.NewGuid();
        var stock = StockPart.Create(partId, 10);
        await repository.AddAsync(stock, CancellationToken.None);

        var result = await repository.GetByPartIdAsync(partId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(stock.Id, result!.Id);
    }

    [Fact]
    public async Task UpdateAsync_should_persist_changes()
    {
        await using var context = CreateContext();
        var repository = new StockPartRepository(context);
        var stock = StockPart.Create(Guid.NewGuid(), 10);
        await repository.AddAsync(stock, CancellationToken.None);

        stock.AddQuantity(5);
        await repository.UpdateAsync(stock, CancellationToken.None);

        var result = await repository.GetByIdAsync(stock.Id, CancellationToken.None);
        Assert.Equal(15, result!.Quantity);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"oficina-stocks-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }
}

using Oficina.Application.Common;
using Oficina.Application.Parts;
using Oficina.Application.Stocks;
using Oficina.Domain.Parts;
using Oficina.Domain.Stock;

namespace Oficina.Tests.Application;

public sealed class StockServiceTests
{
    [Fact]
    public async Task ListAsync_should_return_registered_stocks_ordered_by_part()
    {
        var parts = new FakePartRepository();
        var stocks = new FakeStockPartRepository();
        var part = await AddActivePartAsync(parts, "FLT-100");
        await stocks.AddAsync(StockPart.Create(part.Id, 15), CancellationToken.None);
        var service = new StockService(stocks, parts);

        var result = await service.ListAsync(new PageRequest(), CancellationToken.None);

        Assert.Collection(result.Items, item => Assert.Equal(part.Id, item.PartId));
    }

    [Fact]
    public async Task GetByIdAsync_should_return_null_when_stock_does_not_exist()
    {
        var parts = new FakePartRepository();
        var stocks = new FakeStockPartRepository();
        var service = new StockService(stocks, parts);

        var result = await service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_should_return_stock_when_it_exists()
    {
        var parts = new FakePartRepository();
        var stocks = new FakeStockPartRepository();
        var part = await AddActivePartAsync(parts, "FLT-101");
        var stock = StockPart.Create(part.Id, 8);
        await stocks.AddAsync(stock, CancellationToken.None);
        var service = new StockService(stocks, parts);

        var result = await service.GetByIdAsync(stock.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(8, result!.Quantity);
    }

    [Fact]
    public async Task CreateAsync_should_throw_conflict_when_stock_for_part_already_exists()
    {
        var parts = new FakePartRepository();
        var stocks = new FakeStockPartRepository();
        var part = await AddActivePartAsync(parts, "FLT-001");
        var service = new StockService(stocks, parts);

        await service.CreateAsync(new CreateStockRequest(part.Id, 10), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CreateStockRequest(part.Id, 5), CancellationToken.None));
    }

    [Fact]
    public async Task EntryAsync_should_create_stock_when_it_does_not_exist()
    {
        var parts = new FakePartRepository();
        var stocks = new FakeStockPartRepository();
        var part = await AddActivePartAsync(parts, "FLT-002");
        var service = new StockService(stocks, parts);

        var stock = await service.EntryAsync(part.Id, new StockMovementRequest(7), CancellationToken.None);

        Assert.Equal(part.Id, stock.PartId);
        Assert.Equal(7, stock.Quantity);
    }

    [Fact]
    public async Task ConsumeAsync_should_reduce_existing_stock_quantity()
    {
        var parts = new FakePartRepository();
        var stocks = new FakeStockPartRepository();
        var part = await AddActivePartAsync(parts, "FLT-003");
        await stocks.AddAsync(StockPart.Create(part.Id, 10), CancellationToken.None);
        var service = new StockService(stocks, parts);

        var stock = await service.ConsumeAsync(part.Id, new StockMovementRequest(3), CancellationToken.None);

        Assert.Equal(7, stock.Quantity);
    }

    [Fact]
    public async Task AdjustAsync_should_replace_existing_stock_quantity()
    {
        var parts = new FakePartRepository();
        var stocks = new FakeStockPartRepository();
        var part = await AddActivePartAsync(parts, "FLT-004");
        await stocks.AddAsync(StockPart.Create(part.Id, 10), CancellationToken.None);
        var service = new StockService(stocks, parts);

        var stock = await service.AdjustAsync(part.Id, new StockMovementRequest(7), CancellationToken.None);

        Assert.Equal(7, stock.Quantity);
    }

    [Fact]
    public async Task EntryAsync_should_reject_negative_quantity()
    {
        var parts = new FakePartRepository();
        var stocks = new FakeStockPartRepository();
        var part = await AddActivePartAsync(parts, "FLT-005");
        var service = new StockService(stocks, parts);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.EntryAsync(part.Id, new StockMovementRequest(-3), CancellationToken.None));
    }

    [Fact]
    public async Task ConsumeAsync_should_reject_negative_quantity()
    {
        var parts = new FakePartRepository();
        var stocks = new FakeStockPartRepository();
        var part = await AddActivePartAsync(parts, "FLT-006");
        var service = new StockService(stocks, parts);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.ConsumeAsync(part.Id, new StockMovementRequest(-3), CancellationToken.None));
    }

    [Fact]
    public async Task AdjustAsync_should_reject_negative_quantity()
    {
        var parts = new FakePartRepository();
        var stocks = new FakeStockPartRepository();
        var part = await AddActivePartAsync(parts, "FLT-007");
        var service = new StockService(stocks, parts);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.AdjustAsync(part.Id, new StockMovementRequest(-3), CancellationToken.None));
    }

    private static async Task<Part> AddActivePartAsync(FakePartRepository repository, string code)
    {
        var part = Part.Create("Filtro", code, 35.5m, EnumPartKind.Part);
        await repository.AddAsync(part, CancellationToken.None);
        return part;
    }

    private sealed class FakePartRepository : IPartRepository
    {
        private readonly Dictionary<Guid, Part> _parts = [];

        public Task<List<Part>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_parts.Values.ToList());

        public Task<List<Part>> GetAllById(List<Guid> ids, CancellationToken cancellationToken) =>
            Task.FromResult(_parts.Values.Where(part => ids.Contains(part.Id)).ToList());

        public Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_parts.GetValueOrDefault(id));

        public Task<Part?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(_parts.Values.FirstOrDefault(part =>
                string.Equals(part.Code, code.Trim(), StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(Part part, CancellationToken cancellationToken)
        {
            _parts.Add(part.Id, part);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Part part, CancellationToken cancellationToken)
        {
            _parts[part.Id] = part;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStockPartRepository : IStockRepository
    {
        private readonly Dictionary<Guid, StockPart> _stocks = [];

        public Task<IReadOnlyCollection<StockPart>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<StockPart>>(_stocks.Values.ToList());

        public Task<StockPart?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_stocks.GetValueOrDefault(id));

        public Task<StockPart?> GetByPartIdAsync(Guid partId, CancellationToken cancellationToken) =>
            Task.FromResult(_stocks.Values.FirstOrDefault(stock => stock.PartId == partId));

        public Task AddAsync(StockPart stockPart, CancellationToken cancellationToken)
        {
            _stocks.Add(stockPart.Id, stockPart);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(StockPart stockPart, CancellationToken cancellationToken)
        {
            _stocks[stockPart.Id] = stockPart;
            return Task.CompletedTask;
        }
    }
}

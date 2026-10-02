using Oficina.Application.Common;
using Oficina.Application.Parts;
using Oficina.Application.Parts.UseCases;
using Oficina.Application.Parts.UseCases.Queries;
using Oficina.Application.Stocks;
using Oficina.Domain.Parts;
using Oficina.Domain.Stock;

namespace Oficina.Tests.Application;

public sealed class PartUseCasesTests
{
    [Fact]
    public async Task CreatePart_should_create_part_and_zeroed_stock()
    {
        var parts = new FakePartRepository();
        var stocks = new FakeStockPartRepository();
        var useCase = new CreatePartUseCase(parts, stocks);

        var response = await useCase.CreateAsync(
            new CreatePartRequest("Filtro", "COD-001", 10m, EnumPartKind.Part),
            CancellationToken.None);

        var stock = await stocks.GetByPartIdAsync(response.Id, CancellationToken.None);
        Assert.NotNull(stock);
        Assert.Equal(0, stock!.Quantity);
    }

    [Fact]
    public async Task CreatePart_should_throw_conflict_when_code_already_exists()
    {
        var parts = new FakePartRepository();
        var stocks = new FakeStockPartRepository();
        var useCase = new CreatePartUseCase(parts, stocks);
        await useCase.CreateAsync(new CreatePartRequest("Filtro", "COD-001", 10m, EnumPartKind.Part), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() => useCase.CreateAsync(
            new CreatePartRequest("Outro filtro", "COD-001", 20m, EnumPartKind.Part),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePart_should_throw_conflict_when_code_belongs_to_another_part()
    {
        var parts = new FakePartRepository();
        var stocks = new FakeStockPartRepository();
        var createPart = new CreatePartUseCase(parts, stocks);
        var useCase = new UpdatePartUseCase(parts, new PartService(parts));
        var partA = await createPart.CreateAsync(new CreatePartRequest("Filtro A", "COD-001", 10m, EnumPartKind.Part), CancellationToken.None);
        await createPart.CreateAsync(new CreatePartRequest("Filtro B", "COD-002", 15m, EnumPartKind.Part), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() => useCase.UpdateAsync(
            partA.Id,
            new UpdatePartRequest("Filtro A", "COD-002", 10m, EnumPartKind.Part),
            CancellationToken.None));
    }

    [Fact]
    public async Task AdjustPartStock_should_reject_empty_reason()
    {
        var parts = new FakePartRepository();
        var stocks = new FakeStockPartRepository();
        var createPart = new CreatePartUseCase(parts, stocks);
        var useCase = new AdjustPartStockUseCase(stocks, new PartService(parts));
        var part = await createPart.CreateAsync(new CreatePartRequest("Filtro", "COD-001", 10m, EnumPartKind.Part), CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.AdjustStockAsync(
            part.Id, new AdjustStockRequest(5, " "), CancellationToken.None));
    }

    [Fact]
    public async Task AdjustPartStock_should_create_stock_when_it_does_not_exist()
    {
        var parts = new FakePartRepository();
        var stocks = new FakeStockPartRepository();
        var part = Part.Create("Filtro", "COD-001", 10m, EnumPartKind.Part);
        await parts.AddAsync(part, CancellationToken.None);
        var useCase = new AdjustPartStockUseCase(stocks, new PartService(parts));

        var response = await useCase.AdjustStockAsync(part.Id, new AdjustStockRequest(5, "Contagem inicial"), CancellationToken.None);

        var stock = await stocks.GetByPartIdAsync(part.Id, CancellationToken.None);
        Assert.NotNull(response);
        Assert.Equal(5, stock!.Quantity);
    }

    [Fact]
    public async Task AdjustPartStock_should_update_existing_stock()
    {
        var parts = new FakePartRepository();
        var stocks = new FakeStockPartRepository();
        var createPart = new CreatePartUseCase(parts, stocks);
        var useCase = new AdjustPartStockUseCase(stocks, new PartService(parts));
        var part = await createPart.CreateAsync(new CreatePartRequest("Filtro", "COD-001", 10m, EnumPartKind.Part), CancellationToken.None);

        await useCase.AdjustStockAsync(part.Id, new AdjustStockRequest(8, "Entrada"), CancellationToken.None);

        var stock = await stocks.GetByPartIdAsync(part.Id, CancellationToken.None);
        Assert.Equal(8, stock!.Quantity);
    }

    [Fact]
    public async Task ListParts_should_return_only_active_parts_matching_search()
    {
        var parts = new FakePartRepository();
        var stocks = new FakeStockPartRepository();
        var createPart = new CreatePartUseCase(parts, stocks);
        var deletePart = new DeletePartUseCase(parts);
        var useCase = new ListPartsUseCase(parts);
        var active = await createPart.CreateAsync(new CreatePartRequest("Filtro de oleo", "COD-001", 10m, EnumPartKind.Part), CancellationToken.None);
        var inactive = await createPart.CreateAsync(new CreatePartRequest("Vela", "COD-002", 5m, EnumPartKind.Part), CancellationToken.None);
        await deletePart.DeleteAsync(inactive.Id, CancellationToken.None);

        var result = await useCase.ListAsync(new PageRequest(Search: "filtro"), CancellationToken.None);

        Assert.Equal(active.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task UpdatePart_should_throw_when_part_does_not_exist()
    {
        var parts = new FakePartRepository();
        var useCase = new UpdatePartUseCase(parts, new PartService(parts));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => useCase.UpdateAsync(
            Guid.NewGuid(), new UpdatePartRequest("Filtro", "COD-001", 10m, EnumPartKind.Part), CancellationToken.None));
    }

    [Fact]
    public async Task AdjustPartStock_should_throw_when_part_does_not_exist()
    {
        var parts = new FakePartRepository();
        var stocks = new FakeStockPartRepository();
        var useCase = new AdjustPartStockUseCase(stocks, new PartService(parts));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => useCase.AdjustStockAsync(
            Guid.NewGuid(), new AdjustStockRequest(5, "Contagem"), CancellationToken.None));
    }

    [Fact]
    public async Task DeletePart_should_return_false_when_part_does_not_exist()
    {
        var parts = new FakePartRepository();
        var useCase = new DeletePartUseCase(parts);

        var result = await useCase.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task DeletePart_should_deactivate_existing_part()
    {
        var parts = new FakePartRepository();
        var stocks = new FakeStockPartRepository();
        var createPart = new CreatePartUseCase(parts, stocks);
        var getPartById = new GetPartByIdUseCase(parts);
        var useCase = new DeletePartUseCase(parts);
        var part = await createPart.CreateAsync(new CreatePartRequest("Filtro", "COD-001", 10m, EnumPartKind.Part), CancellationToken.None);

        var result = await useCase.DeleteAsync(part.Id, CancellationToken.None);
        var afterDelete = await getPartById.GetByIdAsync(part.Id, CancellationToken.None);

        Assert.True(result);
        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task GetPartById_should_return_response_for_active_part()
    {
        var parts = new FakePartRepository();
        var part = Part.Create("Filtro", "COD-001", 10m, EnumPartKind.Part);
        await parts.AddAsync(part, CancellationToken.None);
        var useCase = new GetPartByIdUseCase(parts);

        var response = await useCase.GetByIdAsync(part.Id, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(part.Id, response.Id);
        Assert.Equal(part.Name, response.Name);
        Assert.Equal(part.Code, response.Code);
        Assert.Equal(part.UnitPrice, response.UnitPrice);
        Assert.Equal(part.Kind, response.Kind);
    }

    [Fact]
    public async Task GetPartById_should_return_null_when_part_does_not_exist()
    {
        var useCase = new GetPartByIdUseCase(new FakePartRepository());

        var response = await useCase.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(response);
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

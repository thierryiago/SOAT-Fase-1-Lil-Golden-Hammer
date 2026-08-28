using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Parts;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Tests.Infrastructure;

public sealed class PartRepositoryTests
{
    [Fact]
    public async Task AddAsync_and_ListAsync_should_persist_and_return_part()
    {
        await using var context = CreateContext();
        var repository = new PartRepository(context);
        var part = Part.Create("Filtro", "COD-001", 10m, EnumPartKind.Part);

        await repository.AddAsync(part, CancellationToken.None);
        var result = await repository.ListAsync(CancellationToken.None);

        Assert.Equal(part.Id, Assert.Single(result).Id);
    }

    [Fact]
    public async Task GetByIdAsync_should_return_null_when_part_does_not_exist()
    {
        await using var context = CreateContext();
        var repository = new PartRepository(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCodeAsync_should_find_part_by_code()
    {
        await using var context = CreateContext();
        var repository = new PartRepository(context);
        var part = Part.Create("Filtro", "COD-001", 10m, EnumPartKind.Part);
        await repository.AddAsync(part, CancellationToken.None);

        var result = await repository.GetByCodeAsync("COD-001", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(part.Id, result!.Id);
    }

    [Fact]
    public async Task GetAllById_should_return_only_matching_parts()
    {
        await using var context = CreateContext();
        var repository = new PartRepository(context);
        var partA = Part.Create("Filtro", "COD-001", 10m, EnumPartKind.Part);
        var partB = Part.Create("Vela", "COD-002", 5m, EnumPartKind.Part);
        await repository.AddAsync(partA, CancellationToken.None);
        await repository.AddAsync(partB, CancellationToken.None);

        var result = await repository.GetAllById([partA.Id], CancellationToken.None);

        Assert.Equal(partA.Id, Assert.Single(result).Id);
    }

    [Fact]
    public async Task UpdateAsync_should_persist_changes()
    {
        await using var context = CreateContext();
        var repository = new PartRepository(context);
        var part = Part.Create("Filtro", "COD-001", 10m, EnumPartKind.Part);
        await repository.AddAsync(part, CancellationToken.None);

        part.Update("Filtro de Ar", "COD-002", 15m, EnumPartKind.Consumable);
        await repository.UpdateAsync(part, CancellationToken.None);

        var result = await repository.GetByIdAsync(part.Id, CancellationToken.None);
        Assert.Equal("Filtro de Ar", result!.Name);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"oficina-parts-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }
}

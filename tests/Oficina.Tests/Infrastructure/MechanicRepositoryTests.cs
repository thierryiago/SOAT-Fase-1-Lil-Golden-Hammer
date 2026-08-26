using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Mechanics;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Tests.Infrastructure;

public sealed class MechanicRepositoryTests
{
    [Fact]
    public async Task AddAsync_and_ListAsync_should_persist_and_return_mechanic()
    {
        await using var context = CreateContext();
        var repository = new MechanicRepository(context);
        var mechanic = Mechanic.Create("Joao");

        await repository.AddAsync(mechanic, CancellationToken.None);
        var result = await repository.ListAsync(CancellationToken.None);

        Assert.Collection(result, item => Assert.Equal(mechanic.Id, item.Id));
    }

    [Fact]
    public async Task GetByIdAsync_should_return_null_when_mechanic_does_not_exist()
    {
        await using var context = CreateContext();
        var repository = new MechanicRepository(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_should_persist_changes()
    {
        await using var context = CreateContext();
        var repository = new MechanicRepository(context);
        var mechanic = Mechanic.Create("Joao");
        await repository.AddAsync(mechanic, CancellationToken.None);

        mechanic.Update("Pedro");
        await repository.UpdateAsync(mechanic, CancellationToken.None);

        var result = await repository.GetByIdAsync(mechanic.Id, CancellationToken.None);
        Assert.Equal("Pedro", result!.Name);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"oficina-mechanics-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }
}

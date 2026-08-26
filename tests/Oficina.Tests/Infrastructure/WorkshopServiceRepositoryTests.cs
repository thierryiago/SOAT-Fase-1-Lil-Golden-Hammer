using Microsoft.EntityFrameworkCore;
using Oficina.Domain.WorkshopServices;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Tests.Infrastructure;

public sealed class WorkshopServiceRepositoryTests
{
    [Fact]
    public async Task AddAsync_and_ListAsync_should_persist_and_return_service_ordered_by_name()
    {
        await using var context = CreateContext();
        var repository = new WorkshopServiceRepository(context);
        var service = WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30);

        await repository.AddAsync(service, CancellationToken.None);
        var result = await repository.ListAsync(CancellationToken.None);

        Assert.Collection(result, item => Assert.Equal(service.Id, item.Id));
    }

    [Fact]
    public async Task GetByIdAsync_should_return_null_when_service_does_not_exist()
    {
        await using var context = CreateContext();
        var repository = new WorkshopServiceRepository(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllById_should_return_only_matching_services()
    {
        await using var context = CreateContext();
        var repository = new WorkshopServiceRepository(context);
        var serviceA = WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30);
        var serviceB = WorkshopService.Create("Alinhamento", "Descricao", 80m, 20);
        await repository.AddAsync(serviceA, CancellationToken.None);
        await repository.AddAsync(serviceB, CancellationToken.None);

        var result = await repository.GetAllById([serviceA.Id], CancellationToken.None);

        Assert.Collection(result, item => Assert.Equal(serviceA.Id, item.Id));
    }

    [Fact]
    public async Task GetByNameAsync_should_find_service_ignoring_case()
    {
        await using var context = CreateContext();
        var repository = new WorkshopServiceRepository(context);
        var service = WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30);
        await repository.AddAsync(service, CancellationToken.None);

        var result = await repository.GetByNameAsync("TROCA DE OLEO", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(service.Id, result!.Id);
    }

    [Fact]
    public async Task UpdateAsync_should_persist_changes()
    {
        await using var context = CreateContext();
        var repository = new WorkshopServiceRepository(context);
        var service = WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30);
        await repository.AddAsync(service, CancellationToken.None);

        service.Update("Troca de oleo sintetico", "Nova descricao", 150m, 40);
        await repository.UpdateAsync(service, CancellationToken.None);

        var result = await repository.GetByIdAsync(service.Id, CancellationToken.None);
        Assert.Equal("Troca de oleo sintetico", result!.Name);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"oficina-workshopservices-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }
}

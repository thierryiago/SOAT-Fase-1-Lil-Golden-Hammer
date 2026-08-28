using Oficina.Application.Common;
using Oficina.Application.WorkshopServices;
using Oficina.Domain.WorkshopServices;

namespace Oficina.Tests.Application;

public sealed class ServiceCatalogServiceTests
{
    [Fact]
    public async Task ListAsync_should_return_only_active_services()
    {
        var repository = new FakeWorkshopServiceRepository();
        var active = WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30);
        var inactive = WorkshopService.Create("Alinhamento", "Descricao", 80m, 20);
        inactive.Deactivate();
        await repository.AddAsync(active, CancellationToken.None);
        await repository.AddAsync(inactive, CancellationToken.None);
        var service = new ServiceCatalogService(repository);

        var result = await service.ListAsync(new PageRequest(), CancellationToken.None);

        Assert.Equal(active.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task CreateAsync_should_throw_conflict_when_name_already_exists()
    {
        var repository = new FakeWorkshopServiceRepository();
        var service = new ServiceCatalogService(repository);
        await service.CreateAsync(new CreateWorkshopServiceRequest("Troca de oleo", "Descricao", 100m, 30), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(
            new CreateWorkshopServiceRequest("Troca de oleo", "Outra descricao", 120m, 40),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_should_change_service_data()
    {
        var repository = new FakeWorkshopServiceRepository();
        var service = new ServiceCatalogService(repository);
        var created = await service.CreateAsync(new CreateWorkshopServiceRequest("Troca de oleo", "Descricao", 100m, 30), CancellationToken.None);

        var response = await service.UpdateAsync(
            created.Id,
            new UpdateWorkshopServiceRequest("Troca de oleo sintetico", "Nova descricao", 150m, 40),
            CancellationToken.None);

        Assert.Equal("Troca de oleo sintetico", response.Name);
        Assert.Equal(150m, response.UnitPrice);
    }

    [Fact]
    public async Task DeleteAsync_should_return_false_when_service_does_not_exist()
    {
        var repository = new FakeWorkshopServiceRepository();
        var service = new ServiceCatalogService(repository);

        var result = await service.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateAsync_should_throw_conflict_when_name_belongs_to_another_service()
    {
        var repository = new FakeWorkshopServiceRepository();
        var service = new ServiceCatalogService(repository);
        var serviceA = await service.CreateAsync(new CreateWorkshopServiceRequest("Troca de oleo", "Descricao", 100m, 30), CancellationToken.None);
        await service.CreateAsync(new CreateWorkshopServiceRequest("Alinhamento", "Descricao", 80m, 20), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateAsync(
            serviceA.Id,
            new UpdateWorkshopServiceRequest("Alinhamento", "Descricao", 100m, 30),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_should_throw_when_service_does_not_exist()
    {
        var repository = new FakeWorkshopServiceRepository();
        var service = new ServiceCatalogService(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateAsync(
            Guid.NewGuid(),
            new UpdateWorkshopServiceRequest("Troca de oleo", "Descricao", 100m, 30),
            CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_should_deactivate_existing_service()
    {
        var repository = new FakeWorkshopServiceRepository();
        var service = new ServiceCatalogService(repository);
        var created = await service.CreateAsync(new CreateWorkshopServiceRequest("Troca de oleo", "Descricao", 100m, 30), CancellationToken.None);

        var result = await service.DeleteAsync(created.Id, CancellationToken.None);
        var afterDelete = await service.GetByIdAsync(created.Id, CancellationToken.None);

        Assert.True(result);
        Assert.Null(afterDelete);
    }

    private sealed class FakeWorkshopServiceRepository : IWorkshopServiceRepository
    {
        private readonly Dictionary<Guid, WorkshopService> _services = [];

        public Task<IReadOnlyCollection<WorkshopService>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<WorkshopService>>(_services.Values.ToList());

        public Task<List<WorkshopService>> GetAllById(List<Guid> ids, CancellationToken cancellationToken) =>
            Task.FromResult(_services.Values.Where(service => ids.Contains(service.Id)).ToList());

        public Task<WorkshopService?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_services.GetValueOrDefault(id));

        public Task<WorkshopService?> GetByNameAsync(string name, CancellationToken cancellationToken) =>
            Task.FromResult(_services.Values.FirstOrDefault(service =>
                string.Equals(service.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(WorkshopService service, CancellationToken cancellationToken)
        {
            _services.Add(service.Id, service);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(WorkshopService service, CancellationToken cancellationToken)
        {
            _services[service.Id] = service;
            return Task.CompletedTask;
        }
    }
}

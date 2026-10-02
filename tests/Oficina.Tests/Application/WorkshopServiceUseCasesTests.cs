using Oficina.Application.Common;
using Oficina.Application.WorkshopServices;
using Oficina.Application.WorkshopServices.UseCases;
using Oficina.Application.WorkshopServices.UseCases.Queries;
using Oficina.Domain.WorkshopServices;

namespace Oficina.Tests.Application;

public sealed class WorkshopServiceUseCasesTests
{
    [Fact]
    public async Task ListWorkshopServices_should_return_only_active_services()
    {
        var repository = new FakeWorkshopServiceRepository();
        var active = WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30);
        var inactive = WorkshopService.Create("Alinhamento", "Descricao", 80m, 20);
        inactive.Deactivate();
        await repository.AddAsync(active, CancellationToken.None);
        await repository.AddAsync(inactive, CancellationToken.None);
        var useCase = new ListWorkshopServicesUseCase(repository);

        var result = await useCase.ListAsync(new PageRequest(), CancellationToken.None);

        Assert.Equal(active.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task GetWorkshopServiceById_should_return_response_for_active_service()
    {
        var repository = new FakeWorkshopServiceRepository();
        var service = WorkshopService.Create("Troca de oleo", "Descricao", 100m, 30);
        await repository.AddAsync(service, CancellationToken.None);
        var useCase = new GetWorkshopServiceByIdUseCase(repository);

        var response = await useCase.GetByIdAsync(service.Id, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(service.Id, response.Id);
        Assert.Equal(service.Name, response.Name);
        Assert.Equal(service.Description, response.Description);
        Assert.Equal(service.UnitPrice, response.UnitPrice);
        Assert.Equal(service.EstimatedDurationMinutes, response.EstimatedDurationMinutes);
    }

    [Fact]
    public async Task CreateWorkshopService_should_add_new_service()
    {
        var repository = new FakeWorkshopServiceRepository();
        var useCase = new CreateWorkshopServiceUseCase(repository);

        var response = await useCase.CreateAsync(
            new CreateWorkshopServiceRequest("Troca de oleo", "Descricao", 100m, 30),
            CancellationToken.None);

        var stored = await repository.GetByIdAsync(response.Id, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal("Troca de oleo", response.Name);
        Assert.Equal(100m, response.UnitPrice);
    }

    [Fact]
    public async Task CreateWorkshopService_should_throw_conflict_when_name_already_exists()
    {
        var repository = new FakeWorkshopServiceRepository();
        var useCase = new CreateWorkshopServiceUseCase(repository);
        await useCase.CreateAsync(new CreateWorkshopServiceRequest("Troca de oleo", "Descricao", 100m, 30), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() => useCase.CreateAsync(
            new CreateWorkshopServiceRequest("Troca de oleo", "Outra descricao", 120m, 40),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateWorkshopService_should_change_service_data()
    {
        var repository = new FakeWorkshopServiceRepository();
        var createService = new CreateWorkshopServiceUseCase(repository);
        var useCase = new UpdateWorkshopServiceUseCase(repository);
        var created = await createService.CreateAsync(new CreateWorkshopServiceRequest("Troca de oleo", "Descricao", 100m, 30), CancellationToken.None);

        var response = await useCase.UpdateAsync(
            created.Id,
            new UpdateWorkshopServiceRequest("Troca de oleo sintetico", "Nova descricao", 150m, 40),
            CancellationToken.None);

        Assert.Equal("Troca de oleo sintetico", response.Name);
        Assert.Equal(150m, response.UnitPrice);
    }

    [Fact]
    public async Task DeleteWorkshopService_should_return_false_when_service_does_not_exist()
    {
        var repository = new FakeWorkshopServiceRepository();
        var useCase = new DeleteWorkshopServiceUseCase(repository);

        var result = await useCase.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateWorkshopService_should_throw_conflict_when_name_belongs_to_another_service()
    {
        var repository = new FakeWorkshopServiceRepository();
        var createService = new CreateWorkshopServiceUseCase(repository);
        var useCase = new UpdateWorkshopServiceUseCase(repository);
        var serviceA = await createService.CreateAsync(new CreateWorkshopServiceRequest("Troca de oleo", "Descricao", 100m, 30), CancellationToken.None);
        await createService.CreateAsync(new CreateWorkshopServiceRequest("Alinhamento", "Descricao", 80m, 20), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() => useCase.UpdateAsync(
            serviceA.Id,
            new UpdateWorkshopServiceRequest("Alinhamento", "Descricao", 100m, 30),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateWorkshopService_should_throw_when_service_does_not_exist()
    {
        var repository = new FakeWorkshopServiceRepository();
        var useCase = new UpdateWorkshopServiceUseCase(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => useCase.UpdateAsync(
            Guid.NewGuid(),
            new UpdateWorkshopServiceRequest("Troca de oleo", "Descricao", 100m, 30),
            CancellationToken.None));
    }

    [Fact]
    public async Task DeleteWorkshopService_should_deactivate_existing_service()
    {
        var repository = new FakeWorkshopServiceRepository();
        var createService = new CreateWorkshopServiceUseCase(repository);
        var getServiceById = new GetWorkshopServiceByIdUseCase(repository);
        var useCase = new DeleteWorkshopServiceUseCase(repository);
        var created = await createService.CreateAsync(new CreateWorkshopServiceRequest("Troca de oleo", "Descricao", 100m, 30), CancellationToken.None);

        var result = await useCase.DeleteAsync(created.Id, CancellationToken.None);
        var afterDelete = await getServiceById.GetByIdAsync(created.Id, CancellationToken.None);

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

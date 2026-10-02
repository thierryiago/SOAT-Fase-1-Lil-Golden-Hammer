using Oficina.Application.Common;
using Oficina.Application.Mechanics;
using Oficina.Application.Mechanics.UseCases;
using Oficina.Application.Mechanics.UseCases.Queries;
using Oficina.Domain.Mechanics;

namespace Oficina.Tests.Application;

public sealed class MechanicUseCasesTests
{
    [Fact]
    public async Task ListMechanics_should_return_only_active_mechanics()
    {
        var repository = new FakeMechanicRepository();
        var active = Mechanic.Create("Joao");
        var inactive = Mechanic.Create("Pedro");
        inactive.Deactivate();
        await repository.AddAsync(active, CancellationToken.None);
        await repository.AddAsync(inactive, CancellationToken.None);
        var useCase = new ListMechanicsUseCase(repository);

        var result = await useCase.ListAsync(new PageRequest(), CancellationToken.None);

        Assert.Equal(active.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task GetMechanicById_should_return_null_for_inactive_mechanic()
    {
        var repository = new FakeMechanicRepository();
        var mechanic = Mechanic.Create("Joao");
        mechanic.Deactivate();
        await repository.AddAsync(mechanic, CancellationToken.None);
        var useCase = new GetMechanicByIdUseCase(repository);

        var result = await useCase.GetByIdAsync(mechanic.Id, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateMechanic_should_add_new_mechanic()
    {
        var repository = new FakeMechanicRepository();
        var useCase = new CreateMechanicUseCase(repository);

        var response = await useCase.CreateAsync(new CreateMechanicRequest("Joao"), CancellationToken.None);

        Assert.Equal("Joao", response.Name);
    }

    [Fact]
    public async Task UpdateMechanic_should_change_mechanic_name()
    {
        var repository = new FakeMechanicRepository();
        var mechanic = Mechanic.Create("Joao");
        await repository.AddAsync(mechanic, CancellationToken.None);
        var useCase = new UpdateMechanicUseCase(repository);

        var response = await useCase.UpdateAsync(mechanic.Id, new UpdateMechanicRequest("Pedro"), CancellationToken.None);

        Assert.Equal("Pedro", response.Name);
    }

    [Fact]
    public async Task UpdateMechanic_should_throw_when_mechanic_does_not_exist()
    {
        var repository = new FakeMechanicRepository();
        var useCase = new UpdateMechanicUseCase(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => useCase.UpdateAsync(
            Guid.NewGuid(), new UpdateMechanicRequest("Pedro"), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteMechanic_should_deactivate_existing_mechanic()
    {
        var repository = new FakeMechanicRepository();
        var mechanic = Mechanic.Create("Joao");
        await repository.AddAsync(mechanic, CancellationToken.None);
        var useCase = new DeleteMechanicUseCase(repository);

        var result = await useCase.DeleteAsync(mechanic.Id, CancellationToken.None);

        Assert.True(result);
        Assert.False(mechanic.IsActive);
    }

    [Fact]
    public async Task DeleteMechanic_should_return_false_when_mechanic_does_not_exist()
    {
        var repository = new FakeMechanicRepository();
        var useCase = new DeleteMechanicUseCase(repository);

        var result = await useCase.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result);
    }

    private sealed class FakeMechanicRepository : IMechanicRepository
    {
        private readonly Dictionary<Guid, Mechanic> _mechanics = [];

        public Task<List<Mechanic>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_mechanics.Values.ToList());

        public Task<Mechanic?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_mechanics.GetValueOrDefault(id));

        public Task AddAsync(Mechanic mechanic, CancellationToken cancellationToken)
        {
            _mechanics.Add(mechanic.Id, mechanic);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Mechanic mechanic, CancellationToken cancellationToken)
        {
            _mechanics[mechanic.Id] = mechanic;
            return Task.CompletedTask;
        }
    }
}

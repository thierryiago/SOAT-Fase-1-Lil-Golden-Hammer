using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Customers;
using Oficina.Domain.Vehicles;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Tests.Infrastructure;

public sealed class VehicleRepositoryTests
{
    [Fact]
    public async Task AddAsync_and_ListAsync_should_persist_and_return_vehicle()
    {
        await using var context = CreateContext();
        var customer = await AddCustomerAsync(context);
        var repository = new VehicleRepository(context);
        var vehicle = Vehicle.Create(customer.Id, "ABC1234", "Honda", "Civic", 2020, EnumVehicleCategory.Car);

        await repository.AddAsync(vehicle, CancellationToken.None);
        var result = await repository.ListAsync(CancellationToken.None);

        Assert.Equal(vehicle.Id, Assert.Single(result).Id);
    }

    [Fact]
    public async Task GetByIdAsync_should_return_null_when_vehicle_does_not_exist()
    {
        await using var context = CreateContext();
        var repository = new VehicleRepository(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPlateAsync_should_find_vehicle_by_plate()
    {
        await using var context = CreateContext();
        var customer = await AddCustomerAsync(context);
        var repository = new VehicleRepository(context);
        var vehicle = Vehicle.Create(customer.Id, "ABC1234", "Honda", "Civic", 2020, EnumVehicleCategory.Car);
        await repository.AddAsync(vehicle, CancellationToken.None);

        var result = await repository.GetByPlateAsync("ABC-1234", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(vehicle.Id, result!.Id);
    }

    [Fact]
    public async Task UpdateAsync_should_persist_changes()
    {
        await using var context = CreateContext();
        var customer = await AddCustomerAsync(context);
        var repository = new VehicleRepository(context);
        var vehicle = Vehicle.Create(customer.Id, "ABC1234", "Honda", "Civic", 2020, EnumVehicleCategory.Car);
        await repository.AddAsync(vehicle, CancellationToken.None);

        vehicle.Update("XYZ9876", "Yamaha", "Fazer", 2022, EnumVehicleCategory.Motorcycle);
        await repository.UpdateAsync(vehicle, CancellationToken.None);

        var result = await repository.GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal("Yamaha", result!.Brand);
    }

    private static async Task<Customer> AddCustomerAsync(AppDbContext context)
    {
        var customer = Customer.Create("Ana Silva", "ana@email.com", "11999990000", "11144477735");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"oficina-vehicles-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }
}

namespace Oficina.Tests.Application;

public sealed class VehicleServiceTests
{
    /*
    [Fact]
    public async Task Vehicle_crud_should_require_customer_and_enforce_unique_plate()
    {
        var customerRepository = new InMemoryCustomerRepository();
        var vehicleRepository = new InMemoryVehicleRepository();
        var customerService = new CustomerService(customerRepository);
        var vehicleService = new VehicleService(customerRepository, vehicleRepository);
        var customer = await customerService.CreateAsync(
            new CreateCustomerRequest("Joao Silva", "joao@email.com", "123", "111.444.777-35"),
            CancellationToken.None);

        var created = await vehicleService.CreateAsync(
            new CreateVehicleRequest(customer.Id, "abc1d23", "Honda", "Civic", 2021),
            CancellationToken.None);
        var updated = await vehicleService.UpdateAsync(
            created.Id,
            new UpdateVehicleRequest("ABC1D23", "Honda", "Civic Touring", 2022),
            CancellationToken.None);
        var page = await vehicleService.ListAsync(
            new PageRequest("Touring", 1, 20),
            customer.Id,
            CancellationToken.None);

        var duplicateAct = () => vehicleService.CreateAsync(
            new CreateVehicleRequest(customer.Id, "ABC1D23", "Toyota", "Corolla", 2020),
            CancellationToken.None);

        Assert.Equal("ABC1D23", created.Plate);
        Assert.Equal("Civic Touring", updated.Model);
        Assert.Single(page.Items);
        await Assert.ThrowsAsync<ConflictException>(duplicateAct);
        Assert.True(await vehicleService.DeleteAsync(created.Id, CancellationToken.None));
        Assert.Null(await vehicleService.GetByIdAsync(created.Id, CancellationToken.None));
    }

    [Fact]
    public async Task IdentifyCustomerAndRegisterVehicleAsync_should_find_customer_by_document_and_register_vehicle()
    {
        var customers = new InMemoryCustomerRepository();
        var vehicles = new InMemoryVehicleRepository();
        var customerService = new CustomerService(customers);
        var customer = await customerService.CreateAsync(
            new CreateCustomerRequest("Ana Customer", "ana@email.com", "123", "529.982.247-25"),
            CancellationToken.None);
        var service = new VehicleService(customers, vehicles);

        var response = await service.IdentifyCustomerAndRegisterVehicleAsync(
            new IdentifyCustomerAndRegisterVehicleRequest(
                "52998224725",
                "ABC1D23",
                "Toyota",
                "Corolla",
                2022),
            CancellationToken.None);

        Assert.Equal(customer.Id, response.CustomerId);
        Assert.Equal("Ana Customer", response.Name);
        Assert.Equal("529.982.247-25", response.Document);
        Assert.Equal("ABC1D23", response.Vehicle.Plate);
        Assert.Equal("Toyota", response.Vehicle.Brand);
        Assert.Equal("Corolla", response.Vehicle.Model);
        Assert.Equal(2022, response.Vehicle.Year);
    }

    [Fact]
    public async Task IdentifyCustomerAndRegisterVehicleAsync_should_reject_unknown_document()
    {
        var service = new VehicleService(
            new InMemoryCustomerRepository(),
            new InMemoryVehicleRepository());

        var act = () => service.IdentifyCustomerAndRegisterVehicleAsync(
            new IdentifyCustomerAndRegisterVehicleRequest(
                "99999999999",
                "ABC1D23",
                "Toyota",
                "Corolla",
                2022),
            CancellationToken.None);

        await Assert.ThrowsAsync<KeyNotFoundException>(act);
    }
    */
}

using Oficina.Application.Common;
using Oficina.Application.Customers;
using Oficina.Application.Vehicles;
using Oficina.Domain.Customers;
using Oficina.Domain.Vehicles;

namespace Oficina.Tests.Application;

public sealed class VehicleServiceTests
{
    [Fact]
    public async Task CreateAsync_should_throw_when_customer_does_not_exist()
    {
        var customers = new FakeCustomerRepository();
        var vehicles = new FakeVehicleRepository();
        var service = new VehicleService(customers, vehicles);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(
            new CreateVehicleRequest(Guid.NewGuid(), "ABC1234", "Honda", "Civic", 2020),
            CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_should_throw_when_customer_is_inactive()
    {
        var customers = new FakeCustomerRepository();
        var vehicles = new FakeVehicleRepository();
        var customer = await AddCustomerAsync(customers, "11144477735");
        customer.Deactivate();
        var service = new VehicleService(customers, vehicles);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(
            new CreateVehicleRequest(customer.Id, "ABC1234", "Honda", "Civic", 2020),
            CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_should_throw_conflict_when_plate_already_exists()
    {
        var customers = new FakeCustomerRepository();
        var vehicles = new FakeVehicleRepository();
        var customer = await AddCustomerAsync(customers, "11144477735");
        var existingVehicle = Vehicle.Create(customer.Id, "ABC1234", "Honda", "Civic", 2020, EnumVehicleCategory.Car);
        await vehicles.AddAsync(existingVehicle, CancellationToken.None);
        var service = new VehicleService(customers, vehicles);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(
            new CreateVehicleRequest(customer.Id, existingVehicle.Plate, "Honda", "Civic", 2021),
            CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_should_register_vehicle_for_existing_customer()
    {
        var customers = new FakeCustomerRepository();
        var vehicles = new FakeVehicleRepository();
        var customer = await AddCustomerAsync(customers, "11144477735");
        var service = new VehicleService(customers, vehicles);

        var response = await service.CreateAsync(
            new CreateVehicleRequest(customer.Id, "ABC1234", "Honda", "Civic", 2020),
            CancellationToken.None);

        Assert.Equal(customer.Id, response.CustomerId);
        Assert.Equal("ABC-1234", response.Plate);
    }

    [Fact]
    public async Task IdentifyCustomerAndRegisterVehicleAsync_should_throw_when_document_not_found()
    {
        var customers = new FakeCustomerRepository();
        var vehicles = new FakeVehicleRepository();
        var service = new VehicleService(customers, vehicles);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.IdentifyCustomerAndRegisterVehicleAsync(
            new IdentifyCustomerAndRegisterVehicleRequest("11144477735", "ABC1234", "Honda", "Civic", 2020),
            CancellationToken.None));
    }

    [Fact]
    public async Task IdentifyCustomerAndRegisterVehicleAsync_should_register_vehicle_for_found_customer()
    {
        var customers = new FakeCustomerRepository();
        var vehicles = new FakeVehicleRepository();
        var customer = await AddCustomerAsync(customers, "11144477735");
        var service = new VehicleService(customers, vehicles);

        var response = await service.IdentifyCustomerAndRegisterVehicleAsync(
            new IdentifyCustomerAndRegisterVehicleRequest(customer.Document, "ABC1234", "Honda", "Civic", 2020),
            CancellationToken.None);

        Assert.Equal(customer.Id, response.CustomerId);
        Assert.Equal("ABC-1234", response.Vehicle.Plate);
    }

    [Fact]
    public async Task ListAsync_should_filter_by_customer_id()
    {
        var customers = new FakeCustomerRepository();
        var vehicles = new FakeVehicleRepository();
        var customerA = await AddCustomerAsync(customers, "11144477735");
        var customerB = await AddCustomerAsync(customers, "52998224725");
        var vehicleA = Vehicle.Create(customerA.Id, "AAA1111", "Honda", "Civic", 2020, EnumVehicleCategory.Car);
        var vehicleB = Vehicle.Create(customerB.Id, "BBB2222", "Fiat", "Uno", 2019, EnumVehicleCategory.Car);
        await vehicles.AddAsync(vehicleA, CancellationToken.None);
        await vehicles.AddAsync(vehicleB, CancellationToken.None);
        var service = new VehicleService(customers, vehicles);

        var result = await service.ListAsync(new PageRequest(), customerA.Id, CancellationToken.None);

        Assert.Collection(result.Items, item => Assert.Equal(vehicleA.Id, item.Id));
    }

    [Fact]
    public async Task GetByIdAsync_should_return_null_for_inactive_vehicle()
    {
        var customers = new FakeCustomerRepository();
        var vehicles = new FakeVehicleRepository();
        var customer = await AddCustomerAsync(customers, "11144477735");
        var vehicle = Vehicle.Create(customer.Id, "ABC1234", "Honda", "Civic", 2020, EnumVehicleCategory.Car);
        vehicle.Deactivate();
        await vehicles.AddAsync(vehicle, CancellationToken.None);
        var service = new VehicleService(customers, vehicles);

        var result = await service.GetByIdAsync(vehicle.Id, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_should_return_response_for_active_vehicle()
    {
        var customers = new FakeCustomerRepository();
        var vehicles = new FakeVehicleRepository();
        var customer = await AddCustomerAsync(customers, "11144477735");
        var vehicle = Vehicle.Create(customer.Id, "ABC1234", "Honda", "Civic", 2020, EnumVehicleCategory.Car);
        await vehicles.AddAsync(vehicle, CancellationToken.None);
        var service = new VehicleService(customers, vehicles);

        var result = await service.GetByIdAsync(vehicle.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(vehicle.Id, result!.Id);
    }

    [Fact]
    public async Task UpdateAsync_should_change_vehicle_data()
    {
        var customers = new FakeCustomerRepository();
        var vehicles = new FakeVehicleRepository();
        var customer = await AddCustomerAsync(customers, "11144477735");
        var vehicle = Vehicle.Create(customer.Id, "ABC1234", "Honda", "Civic", 2020, EnumVehicleCategory.Car);
        await vehicles.AddAsync(vehicle, CancellationToken.None);
        var service = new VehicleService(customers, vehicles);

        var response = await service.UpdateAsync(
            vehicle.Id,
            new UpdateVehicleRequest("XYZ9876", "Yamaha", "Fazer", 2022, EnumVehicleCategory.Motorcycle),
            CancellationToken.None);

        Assert.Equal("XYZ-9876", response.Plate);
        Assert.Equal("Yamaha", response.Brand);
    }

    [Fact]
    public async Task UpdateAsync_should_throw_when_vehicle_does_not_exist()
    {
        var customers = new FakeCustomerRepository();
        var vehicles = new FakeVehicleRepository();
        var service = new VehicleService(customers, vehicles);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateAsync(
            Guid.NewGuid(),
            new UpdateVehicleRequest("ABC1234", "Honda", "Civic", 2020, EnumVehicleCategory.Car),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_should_throw_conflict_when_plate_belongs_to_another_vehicle()
    {
        var customers = new FakeCustomerRepository();
        var vehicles = new FakeVehicleRepository();
        var customer = await AddCustomerAsync(customers, "11144477735");
        var vehicleA = Vehicle.Create(customer.Id, "AAA1111", "Honda", "Civic", 2020, EnumVehicleCategory.Car);
        var vehicleB = Vehicle.Create(customer.Id, "BBB2222", "Fiat", "Uno", 2019, EnumVehicleCategory.Car);
        await vehicles.AddAsync(vehicleA, CancellationToken.None);
        await vehicles.AddAsync(vehicleB, CancellationToken.None);
        var service = new VehicleService(customers, vehicles);

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateAsync(
            vehicleA.Id,
            new UpdateVehicleRequest(vehicleB.Plate, "Honda", "Civic", 2020, EnumVehicleCategory.Car),
            CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_should_deactivate_existing_vehicle()
    {
        var customers = new FakeCustomerRepository();
        var vehicles = new FakeVehicleRepository();
        var customer = await AddCustomerAsync(customers, "11144477735");
        var vehicle = Vehicle.Create(customer.Id, "ABC1234", "Honda", "Civic", 2020, EnumVehicleCategory.Car);
        await vehicles.AddAsync(vehicle, CancellationToken.None);
        var service = new VehicleService(customers, vehicles);

        var result = await service.DeleteAsync(vehicle.Id, CancellationToken.None);

        Assert.True(result);
        Assert.False(vehicle.IsActive);
    }

    private static async Task<Customer> AddCustomerAsync(FakeCustomerRepository repository, string document)
    {
        var customer = Customer.Create("Ana Silva", "ana@email.com", "11999990000", document);
        await repository.AddAsync(customer, CancellationToken.None);
        return customer;
    }

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        private readonly Dictionary<Guid, Customer> _customers = [];

        public Task<List<Customer>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_customers.Values.ToList());

        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_customers.GetValueOrDefault(id));

        public Task<Customer?> GetByDocumentAsync(string document, CancellationToken cancellationToken) =>
            Task.FromResult(_customers.Values.FirstOrDefault(customer =>
                string.Equals(customer.Document, document, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(Customer customer, CancellationToken cancellationToken)
        {
            _customers.Add(customer.Id, customer);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Customer customer, CancellationToken cancellationToken)
        {
            _customers[customer.Id] = customer;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeVehicleRepository : IVehicleRepository
    {
        private readonly Dictionary<Guid, Vehicle> _vehicles = [];

        public Task<List<Vehicle>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_vehicles.Values.ToList());

        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_vehicles.GetValueOrDefault(id));

        public Task<Vehicle?> GetByPlateAsync(string plate, CancellationToken cancellationToken) =>
            Task.FromResult(_vehicles.Values.FirstOrDefault(vehicle =>
                string.Equals(vehicle.Plate, plate, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            _vehicles.Add(vehicle.Id, vehicle);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            _vehicles[vehicle.Id] = vehicle;
            return Task.CompletedTask;
        }
    }
}

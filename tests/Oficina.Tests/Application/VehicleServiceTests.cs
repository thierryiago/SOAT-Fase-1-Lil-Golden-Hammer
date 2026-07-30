using Oficina.Application.Customers;
using Oficina.Application.Vehicles;
using Oficina.Domain.Customers;

namespace Oficina.Tests.Application;

public sealed class VehicleServiceTests
{
    [Fact]
    public async Task IdentifyCustomerAndRegisterVehicleAsync_should_find_customer_by_document_and_register_vehicle()
    {
        var customers = new FakeCustomerRepository();
        var customer = Customer.Create("Ana Customer", "ana@email.com", "123.456.789-00");
        await customers.AddAsync(customer, CancellationToken.None);
        var service = new VehicleService(customers);

        var response = await service.IdentifyCustomerAndRegisterVehicleAsync(
            new IdentifyCustomerAndRegisterVehicleRequest(
                "12345678900",
                "ABC1D23",
                "Toyota",
                "Corolla",
                2022),
            CancellationToken.None);

        Assert.Equal(customer.Id, response.CustomerId);
        Assert.Equal("Ana Customer", response.Name);
        Assert.Equal("123.456.789-00", response.Document);
        Assert.Equal("ABC1D23", response.Vehicle.Plate);
        Assert.Equal("Toyota", response.Vehicle.Make);
        Assert.Equal("Corolla", response.Vehicle.Model);
        Assert.Equal(2022, response.Vehicle.Year);
    }

    [Fact]
    public async Task IdentifyCustomerAndRegisterVehicleAsync_should_reject_unknown_document()
    {
        var customers = new FakeCustomerRepository();
        var service = new VehicleService(customers);

        var act = () => service.IdentifyCustomerAndRegisterVehicleAsync(
            new IdentifyCustomerAndRegisterVehicleRequest(
                "99999999999",
                "ABC1D23",
                "Toyota",
                "Corolla",
                2022),
            CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        private readonly Dictionary<Guid, Customer> _customers = new();

        public Task<IReadOnlyCollection<Customer>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<Customer>>(_customers.Values.ToList());

        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_customers.GetValueOrDefault(id));

        public Task<Customer?> GetByDocumentAsync(string document, CancellationToken cancellationToken)
        {
            var normalizedDocument = new string(document.Where(char.IsDigit).ToArray());
            var customer = _customers.Values.FirstOrDefault(existingCustomer =>
                new string(existingCustomer.Document.Where(char.IsDigit).ToArray()) == normalizedDocument);

            return Task.FromResult(customer);
        }

        public Task AddAsync(Customer customer, CancellationToken cancellationToken)
        {
            _customers[customer.Id] = customer;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Customer customer, CancellationToken cancellationToken)
        {
            _customers[customer.Id] = customer;
            return Task.CompletedTask;
        }
    }
}

using Oficina.Application.Customers;
using Oficina.Application.Parts;
using Oficina.Application.ServiceOrders;
using Oficina.Domain.Customers;
using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Tests.Application;

public sealed class ServiceOrderServiceTests
{
    [Fact]
    public async Task OpenAsync_should_create_received_service_order_for_existing_customer()
    {
        var customers = new FakeCustomerRepository();
        var serviceOrders = new FakeServiceOrderRepository();
        var parts = new FakePartRepository();
        var customer = Customer.Create("John Customer", "john@email.com", "123");
        await customers.AddAsync(customer, CancellationToken.None);
        var service = new ServiceOrderService(serviceOrders, customers, parts);

        var serviceOrder = await service.OpenAsync(
            new OpenServiceOrderRequest(customer.Id, "Troca de oleo"),
            CancellationToken.None);

        Assert.Equal(customer.Id, serviceOrder.CustomerId);
        Assert.Equal(ServiceOrderStatus.Received, serviceOrder.Status);
        Assert.Equal("Troca de oleo", serviceOrder.Description);
    }

    [Fact]
    public void ServiceOrderStatus_should_have_required_statuses()
    {
        var statuses = Enum.GetNames<ServiceOrderStatus>();

        Assert.Equal(
            new[]
            {
                "Received",
                "InDiagnosis",
                "AwaitingApproval",
                "InExecution",
                "Finalized",
                "Delivered"
            },
            statuses);
    }

    [Fact]
    public async Task AddPartAsync_should_update_service_order_total()
    {
        var customers = new FakeCustomerRepository();
        var serviceOrders = new FakeServiceOrderRepository();
        var parts = new FakePartRepository();
        var customer = Customer.Create("John Customer", "john@email.com", "123");
        var part = Part.Create("Filtro de oleo", "FLT-001", 35.50m, 4);
        await customers.AddAsync(customer, CancellationToken.None);
        await parts.AddAsync(part, CancellationToken.None);
        var service = new ServiceOrderService(serviceOrders, customers, parts);
        var serviceOrder = await service.OpenAsync(
            new OpenServiceOrderRequest(customer.Id, "Revisao preventiva"),
            CancellationToken.None);

        await service.AddPartAsync(
            serviceOrder.Id,
            new AddPartToServiceOrderRequest(part.Id, 2),
            CancellationToken.None);

        var updated = await serviceOrders.GetByIdAsync(serviceOrder.Id, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(71.00m, updated!.TotalParts);
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

    private sealed class FakePartRepository : IPartRepository
    {
        private readonly Dictionary<Guid, Part> _parts = new();

        public Task<IReadOnlyCollection<Part>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<Part>>(_parts.Values.ToList());

        public Task<Part?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_parts.GetValueOrDefault(id));

        public Task<Part?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(_parts.Values.FirstOrDefault(part =>
                string.Equals(part.Code, code, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(Part part, CancellationToken cancellationToken)
        {
            _parts[part.Id] = part;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Part part, CancellationToken cancellationToken)
        {
            _parts[part.Id] = part;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeServiceOrderRepository : IServiceOrderRepository
    {
        private readonly Dictionary<Guid, ServiceOrder> _serviceOrders = new();

        public Task<IReadOnlyCollection<ServiceOrder>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<ServiceOrder>>(_serviceOrders.Values.ToList());

        public Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_serviceOrders.GetValueOrDefault(id));

        public Task AddAsync(ServiceOrder serviceOrder, CancellationToken cancellationToken)
        {
            _serviceOrders[serviceOrder.Id] = serviceOrder;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ServiceOrder serviceOrder, CancellationToken cancellationToken)
        {
            _serviceOrders[serviceOrder.Id] = serviceOrder;
            return Task.CompletedTask;
        }
    }
}

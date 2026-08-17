using Oficina.Domain.Customers;
using Oficina.Domain.Mechanics;
using Oficina.Domain.OrderService;

namespace Oficina.Domain.ServiceOrders;

public sealed class ServiceOrder
{
    private readonly List<ServiceOrderPart> _items = new();

    private ServiceOrder(Guid id, Guid customerId, string description)
    {
        Id = id;
        CustomerId = customerId;
        Description = description;
        Status = 0;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }
    public Guid CustomerId { get; }
    public Guid? MechanicId { get; }
    public Guid? VehicleId { get; }
    public string Description { get; private set; }
    public string? CheckList { get; private set; }
    public ServiceOrderStatus? Status { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public decimal TotalParts { get; private set; }
    public Customer Customer { get; private set; }
    public Mechanic? Mechanic { get; private set; }
    public Vehicle? Vehicle { get; set; }
    public IReadOnlyCollection<ServiceOrderPart> Parts => _items.AsReadOnly();
    public IReadOnlyCollection<ServiceOrderWorkshop> WorkshopServices { get; private set; }
    public static ServiceOrder Open(Guid customerId, string description)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer is required.", nameof(customerId));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Service order description is required.", nameof(description));
        }

        return new ServiceOrder(Guid.NewGuid(), customerId, description.Trim());
    }

    public void AddPart(Guid partId, string partName, int quantity, decimal unitPrice)
    {
        if (Status is ServiceOrderStatus.Finalized or ServiceOrderStatus.Delivered)
        {
            throw new InvalidOperationException("Closed service orders cannot be changed.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");
        }

        _items.Add(new ServiceOrderPart(Guid.NewGuid(), partId, Id, quantity));
        TotalParts += quantity * unitPrice;
    }

    public void Start()
    {
        if (Status != ServiceOrderStatus.Received)
        {
            throw new InvalidOperationException("Only received service orders can be started.");
        }

        Status = ServiceOrderStatus.InExecution;
    }

    public void FinalizeOrder()
    {
        if (Status == ServiceOrderStatus.Delivered)
        {
            throw new InvalidOperationException("Delivered service orders cannot be finalized again.");
        }

        Status = ServiceOrderStatus.Finalized;
    }

    public void Deliver()
    {
        if (Status != ServiceOrderStatus.Finalized)
        {
            throw new InvalidOperationException("Only finalized service orders can be delivered.");
        }

        Status = ServiceOrderStatus.Delivered;
    }
}

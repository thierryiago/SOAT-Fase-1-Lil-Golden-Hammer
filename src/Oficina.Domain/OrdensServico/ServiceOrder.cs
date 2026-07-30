namespace Oficina.Domain.ServiceOrders;

public sealed class ServiceOrder
{
    private readonly List<ServiceOrderItem> _items = new();

    private ServiceOrder(Guid id, Guid customerId, string description)
    {
        Id = id;
        CustomerId = customerId;
        Description = description;
        Status = ServiceOrderStatus.Received;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }
    public Guid CustomerId { get; }
    public string Description { get; private set; }
    public ServiceOrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public IReadOnlyCollection<ServiceOrderItem> Items => _items.AsReadOnly();
    public decimal TotalParts => _items.Sum(item => item.Total);

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

        _items.Add(new ServiceOrderItem(partId, partName, quantity, unitPrice));
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

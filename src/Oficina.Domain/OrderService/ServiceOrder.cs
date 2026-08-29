using Oficina.Domain.Customers;
using Oficina.Domain.Mechanics;
using Oficina.Domain.OrderService;

namespace Oficina.Domain.ServiceOrders;

public sealed class ServiceOrder
{
    private ServiceOrder(Guid id, Guid customerId, Guid? vehicleId,
        string description)
    {
        Id = id;
        CustomerId = customerId;
        VehicleId = vehicleId;
        Description = description;
        Status = null;
        CreatedAt = DateTimeOffset.UtcNow;
        Parts = new List<ServiceOrderPart>();
        WorkshopServices = new List<ServiceOrderWorkshop>();
    }

    public Guid Id { get; }
    public Guid CustomerId { get; }
    public Guid? MechanicId { get; private set; }
    public Guid? VehicleId { get; }
    public string Description { get; private set; }
    public string? CheckList { get; private set; }
    public ServiceOrderStatus? Status { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset ScheduledAt { get; private set; }
    public decimal TotalParts { get; private set; }
    public Customer Customer { get; private set; } = null!;
    public Mechanic? Mechanic { get; private set; }
    public Vehicle? Vehicle { get; set; }
    public IReadOnlyCollection<ServiceOrderPart> Parts { get; private set; }
    public IReadOnlyCollection<ServiceOrderWorkshop> WorkshopServices { get; private set; }

    public static ServiceOrder Open(Guid customerId, Guid vehicleId, string description)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer is required.", nameof(customerId));
        }
        if (vehicleId == Guid.Empty)
        {
            throw new ArgumentException("Customer is required.", nameof(customerId));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Service order description is required.", nameof(description));
        }

        var serviceOrder = new ServiceOrder(Guid.NewGuid(), customerId, vehicleId, description.Trim())
        {
            ScheduledAt = DateTimeOffset.UtcNow,
            Status = ServiceOrderStatus.Created
        };

        return serviceOrder;
    }

    public void Update(
        Guid? mechanicId,
        string? description,
        string? checkList,
        IReadOnlyCollection<ServiceOrderPart>? parts,
        IReadOnlyCollection<ServiceOrderWorkshop>? workshopServices)
    {
        if (checkList is not null)
        {
            CheckList = checkList.Trim();
        }

        if (description is not null)
        {
            Description = description.Trim();
        }
        if (mechanicId is not null)
        {
            MechanicId = mechanicId;
        }

        if (parts is not null)
        {
            SetParts(parts);
        }

        if (workshopServices is not null)
        {
            SetWorkshopServices(workshopServices);
        }
    }

    private void SetParts(IReadOnlyCollection<ServiceOrderPart> parts)
    {
        Parts = parts.ToList();
        TotalParts = Parts.Sum(item => item.QuantityUsed * (item.Part?.UnitPrice ?? 0));
    }

    private void SetWorkshopServices(IReadOnlyCollection<ServiceOrderWorkshop> workshopServices)
    {
        WorkshopServices = workshopServices.ToList();
    }

    public void UpdateStatus(
        bool? clientApproved = null,
        bool finalized = false,
        bool delivered = false)
    {
        if (Status == ServiceOrderStatus.Delivered)
        {
            throw new InvalidOperationException("A delivered service order cannot be changed.");
        }

        if (Status == ServiceOrderStatus.Rejected)
        {
            throw new InvalidOperationException("A rejected service order cannot be changed.");
        }

        if (Receive())
            return;

        if (StartDiagnosis())
            return;

        if (RequestApproval())
            return;

        if (ResolveApproval(clientApproved))
            return;

        if (Finish(finalized))
            return;

        Deliver(delivered);
    }

    private void EnsureNotTerminal()
    {
        if (Status == ServiceOrderStatus.Finalized)
        {
            throw new InvalidOperationException("A finalized service order cannot be changed.");
        }

        if (Status == ServiceOrderStatus.Delivered)
        {
            throw new InvalidOperationException("A delivered service order cannot be changed.");
        }

        if (Status == ServiceOrderStatus.Rejected)
        {
            throw new InvalidOperationException("A rejected service order cannot be changed.");
        }
    }

    private bool Created()
    {
        if (Status is not null)
        {
            return false;
        }

        Status = ServiceOrderStatus.Created;
        return true;
    }

    private bool Receive()
    {
        if (Status is not null || string.IsNullOrWhiteSpace(CheckList))
        {
            return false;
        }

        Status = ServiceOrderStatus.Received;
        return true;
    }

    private bool StartDiagnosis()
    {
        if (Status != ServiceOrderStatus.Received || !MechanicId.HasValue)
        {
            return false;
        }

        Status = ServiceOrderStatus.InDiagnosis;
        return true;
    }

    private bool RequestApproval()
    {
        if (Status != ServiceOrderStatus.InDiagnosis || WorkshopServices.Count == 0)
        {
            return false;
        }

        Status = ServiceOrderStatus.AwaitingApproval;
        return true;
    }

    private bool ResolveApproval(bool? clientApproved)
    {
        if (Status != ServiceOrderStatus.AwaitingApproval || !clientApproved.HasValue)
        {
            return false;
        }

        Status = clientApproved.Value
            ? ServiceOrderStatus.InExecution
            : ServiceOrderStatus.Rejected;
        return true;
    }

    private bool Finish(bool finalized)
    {
        if (Status != ServiceOrderStatus.InExecution || !finalized)
        {
            return false;
        }

        Status = ServiceOrderStatus.Finalized;
        return true;
    }

    private void Deliver(bool delivered)
    {
        if (Status != ServiceOrderStatus.Finalized || !delivered)
        {
            return;
        }

        Status = ServiceOrderStatus.Delivered;
    }

    public void ValidateUpdate(
        Guid? newMechanicId,
        bool hasNewItems)
    {
        EnsureNotTerminal();

        if (Status is
            ServiceOrderStatus.InDiagnosis or
            ServiceOrderStatus.AwaitingApproval or
            ServiceOrderStatus.InExecution)
        {
            if (newMechanicId is not null && newMechanicId != MechanicId)
            {
                throw new InvalidOperationException(
                    "The mechanic cannot be removed or changed at this stage.");
            }
        }

        if (hasNewItems &&
            Status is null or ServiceOrderStatus.Received or ServiceOrderStatus.InExecution)
        {
            throw new InvalidOperationException(
                "Services and parts cannot be added at this stage.");
        }
    }

}

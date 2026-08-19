using Oficina.Domain.Customers;
using Oficina.Domain.Mechanics;
using Oficina.Domain.OrderService;

namespace Oficina.Domain.ServiceOrders;

public sealed class ServiceOrder
{
    private ServiceOrder(Guid id, Guid customerId, string description)
    {
        Id = id;
        CustomerId = customerId;
        Description = description;
        Status = 0;
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

        var serviceOrder = new ServiceOrder(Guid.NewGuid(), customerId, description.Trim())
        {
            ScheduledAt = DateTimeOffset.UtcNow
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
        MechanicId = mechanicId;

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
        bool hasNewItems = false,
        bool newItemsExecuted = false,
        bool finalized = false)
    {
        if (Status == ServiceOrderStatus.Finalized)
        {
            if (hasNewItems)
            {
                throw new InvalidOperationException("A finalized service order cannot receive new services or parts.");
            }

            throw new InvalidOperationException("A finalized service order cannot be changed.");
        }

        if (Status == ServiceOrderStatus.Delivered)
        {
            throw new InvalidOperationException("A delivered service order cannot be changed.");
        }

        if (Status == null)
        {
            SetReceivedStatus();
            return;
        }

        if (Status == ServiceOrderStatus.Received)
        {
            SetInDiagnosisStatus();
            return;
        }

        if (Status == ServiceOrderStatus.InDiagnosis)
        {
            SetAwaitingApprovalStatus();
            return;
        }

        if (Status == ServiceOrderStatus.AwaitingApproval)
        {
            SetApprovalStatus(
                clientApproved,
                newItemsExecuted);

            return;
        }

        if (Status == ServiceOrderStatus.InExecution)
        {
            SetInExecutionStatus(
                hasNewItems,
                finalized);

            return;
        }

        if (Status == ServiceOrderStatus.Rejected)
        {
            throw new InvalidOperationException(
                "A rejected service order cannot be changed.");
        }
    }

    private void SetReceivedStatus()
    {
        if (string.IsNullOrWhiteSpace(CheckList))
        {
            return;
        }

        Status = ServiceOrderStatus.Received;
    }

    private void SetInDiagnosisStatus()
    {
        if (!MechanicId.HasValue)
        {
            return;
        }

        Status = ServiceOrderStatus.InDiagnosis;
    }

    private void SetAwaitingApprovalStatus()
    {
        if (Parts.Count == 0 && WorkshopServices.Count == 0)
        {
            return;
        }

        Status = ServiceOrderStatus.AwaitingApproval;
    }

    private void SetApprovalStatus(
        bool? clientApproved,
        bool newItemsExecuted)
    {
        if (!clientApproved.HasValue)
        {
            throw new InvalidOperationException(
                "The service order is awaiting customer approval.");
        }

        if (clientApproved.Value)
        {
            Status = ServiceOrderStatus.InExecution;
            return;
        }

        if (newItemsExecuted)
        {
            Status = ServiceOrderStatus.Finalized;
            return;
        }

        Status = ServiceOrderStatus.Rejected;
    }

    private void SetInExecutionStatus(
        bool hasNewItems,
        bool finalized)
    {
        if (hasNewItems)
        {
            Status = ServiceOrderStatus.AwaitingApproval;
            return;
        }

        if (finalized)
        {
            Status = ServiceOrderStatus.Finalized;
        }
    }

    public void ValidateUpdate(
        Guid? newMechanicId,
        bool hasNewItems,
        bool? clientApproved)
    {
        if (Status == ServiceOrderStatus.Finalized)
        {
            throw new InvalidOperationException(
                "A finalized service order cannot be changed.");
        }

        if (Status == ServiceOrderStatus.Delivered)
        {
            throw new InvalidOperationException(
                "A delivered service order cannot be changed.");
        }

        if (Status == ServiceOrderStatus.Rejected)
        {
            throw new InvalidOperationException(
                "A rejected service order cannot be changed.");
        }

        if (Status is
            ServiceOrderStatus.InDiagnosis or
            ServiceOrderStatus.AwaitingApproval or
            ServiceOrderStatus.InExecution)
        {
            if (newMechanicId != MechanicId)
            {
                throw new InvalidOperationException(
                    "The mechanic cannot be removed or changed at this stage.");
            }
        }

        if (clientApproved.HasValue &&
            Status != ServiceOrderStatus.AwaitingApproval)
        {
            throw new InvalidOperationException(
                "Customer approval can only be changed when the service order is awaiting approval.");
        }

        if (hasNewItems &&
            Status is null or ServiceOrderStatus.Received)
        {
            throw new InvalidOperationException(
                "Services and parts cannot be added at this stage.");
        }
    }

}

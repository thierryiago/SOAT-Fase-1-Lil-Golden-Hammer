namespace Oficina.Domain.Budget;

public sealed class Budget
{
    private Budget(Guid id, Guid customerId, Guid serviceOrderId)
    {
        Id = id;
        CustomerId = customerId;
        ServiceOrderId = serviceOrderId;
        CreatedAt = DateTimeOffset.UtcNow;
        IsApproved = null;
        TotalValue = 0m;
        Parts = new List<BudgetParts>();
        WorkshopServices = new List<BudgetWorkshopServices>();
    }

    public Guid Id { get; }
    public Guid CustomerId { get; }
    public Guid ServiceOrderId { get; }
    public DateTimeOffset CreatedAt { get; }
    public bool? IsApproved { get; private set; }
    public decimal TotalValue { get; private set; }
    public IReadOnlyCollection<BudgetParts> Parts { get; private set; }
    public IReadOnlyCollection<BudgetWorkshopServices> WorkshopServices { get; private set; }

    public static Budget Open(
        Guid id,
        Guid customerId,
        Guid serviceOrderId,
        IReadOnlyCollection<BudgetParts>? parts,
        IReadOnlyCollection<BudgetWorkshopServices> workshopServices)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id is required.", nameof(id));
        }

        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer is required.", nameof(customerId));
        }

        if (serviceOrderId == Guid.Empty)
        {
            throw new ArgumentException("Service order is required.", nameof(serviceOrderId));
        }

        if (workshopServices is null || workshopServices.Count == 0)
        {
            throw new ArgumentException(
                "The budget must be opened with at least one workshop service.",
                nameof(workshopServices));
        }

        var budget = new Budget(id, customerId, serviceOrderId)
        {
            Parts = parts ?? Array.Empty<BudgetParts>(),
            WorkshopServices = workshopServices
        };
        budget.TotalValue = CalculateTotalValue(budget.Parts, budget.WorkshopServices);

        return budget;
    }

    private static decimal CalculateTotalValue(
        IReadOnlyCollection<BudgetParts> parts,
        IReadOnlyCollection<BudgetWorkshopServices> workshopServices)
    {
        var partsTotal = parts.Sum(item => item.Quantity * (item.Part?.UnitPrice ?? 0));
        var servicesTotal = workshopServices.Sum(item => item.WorkshopService?.UnitPrice ?? 0);
        return partsTotal + servicesTotal;
    }
}

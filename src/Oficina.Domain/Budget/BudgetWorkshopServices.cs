using Oficina.Domain.WorkshopServices;

namespace Oficina.Domain.Budget;

public sealed class BudgetWorkshopServices
{
    private BudgetWorkshopServices(
        Guid id,
        Guid budgetId,
        Guid workshopServiceId,
        string workshopServiceName,
        decimal unitPrice)
    {
        Id = id;
        BudgetId = budgetId;
        WorkshopServiceId = workshopServiceId;
        WorkshopServiceName = workshopServiceName;
        UnitPrice = unitPrice;
    }

    public Guid Id { get; }
    public Guid BudgetId { get; }
    public Guid WorkshopServiceId { get; }
    public string WorkshopServiceName { get; }
    public decimal UnitPrice { get; }
    public Budget? Budget { get; set; }
    public WorkshopService? WorkshopService { get; set; }

    public static BudgetWorkshopServices Create(
        Guid budgetId,
        Guid workshopServiceId,
        string name,
        decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Workshop service name is required.", nameof(name));
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");
        }

        return new BudgetWorkshopServices(
            Guid.NewGuid(),
            budgetId,
            workshopServiceId,
            name.Trim(),
            unitPrice);
    }
}

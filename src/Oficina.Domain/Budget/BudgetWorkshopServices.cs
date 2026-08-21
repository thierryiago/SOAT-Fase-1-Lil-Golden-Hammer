using Oficina.Domain.WorkshopServices;

namespace Oficina.Domain.Budget;

public sealed class BudgetWorkshopServices
{
    private BudgetWorkshopServices(Guid id, Guid budgetId, Guid workshopServiceId)
    {
        Id = id;
        BudgetId = budgetId;
        WorkshopServiceId = workshopServiceId;
    }

    public Guid Id { get; }
    public Guid BudgetId { get; }
    public Guid WorkshopServiceId { get; }
    public Budget? Budget { get; set; }
    public WorkshopService? WorkshopService { get; set; }

    public static BudgetWorkshopServices Create(Guid budgetId, Guid workshopServiceId) =>
        new(Guid.NewGuid(), budgetId, workshopServiceId);
}

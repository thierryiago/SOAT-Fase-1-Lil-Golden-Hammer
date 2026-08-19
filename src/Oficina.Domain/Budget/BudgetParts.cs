using Oficina.Domain.Parts;

namespace Oficina.Domain.Budget;

public sealed class BudgetParts
{
    private BudgetParts(Guid id, Guid budgetId, Guid partId, int quantity)
    {
        Id = id;
        BudgetId = budgetId;
        PartId = partId;
        Quantity = quantity;
    }

    public Guid Id { get; }
    public Guid BudgetId { get; }
    public Guid PartId { get; }
    public int Quantity { get; }
    public Budget? Budget { get; set; }
    public Part? Part { get; set; }

    public static BudgetParts Create(Guid budgetId, Guid partId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        return new BudgetParts(Guid.NewGuid(), budgetId, partId, quantity);
    }
}

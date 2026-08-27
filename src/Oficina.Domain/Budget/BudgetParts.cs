using Oficina.Domain.Parts;

namespace Oficina.Domain.Budget;

public sealed class BudgetParts
{
    private BudgetParts(
        Guid id,
        Guid budgetId,
        Guid partId,
        string partName,
        decimal unitPrice,
        int quantity)
    {
        Id = id;
        BudgetId = budgetId;
        PartId = partId;
        PartName = partName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public Guid Id { get; }
    public Guid BudgetId { get; }
    public Guid PartId { get; }
    public string PartName { get; }
    public decimal UnitPrice { get; }
    public int Quantity { get; }
    public Budget? Budget { get; set; }
    public Part? Part { get; set; }

    public static BudgetParts Create(
        Guid budgetId,
        Guid partId,
        string name,
        decimal unitPrice,
        int quantity)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Part name is required.", nameof(name));
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        return new BudgetParts(Guid.NewGuid(), budgetId, partId, name.Trim(), unitPrice, quantity);
    }
}

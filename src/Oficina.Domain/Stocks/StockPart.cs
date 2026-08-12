using Oficina.Domain.Parts;

namespace Oficina.Domain.Stock;

public sealed class StockPart
{
    private StockPart(Guid partId, int quantity)
    {
        Id = Guid.NewGuid();
        PartId = partId;
        Quantity = quantity;
        CreatedDate = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid PartId { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public int Quantity { get; private set; }
    public Part? Part { get; private set; }

    public static StockPart Create(Guid partId, int quantity)
    {
        if (partId == Guid.Empty)
        {
            throw new ArgumentException("Part reference is required.", nameof(partId));
        }

        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Stock quantity cannot be negative.");
        }

        return new StockPart(partId, quantity);
    }

    public void AddQuantity(int quantity)
    {
        ValidateNonZeroMovement(quantity);
        Quantity += quantity;
    }

    public void RemoveQuantity(int quantity)
    {
        ValidateNonZeroMovement(quantity);
        if (Quantity < quantity)
        {
            throw new InvalidOperationException("Stock quantity cannot be negative.");
        }

        Quantity -= quantity;
    }

    public void AdjustQuantity(int quantity)
    {
        ValidateNonZeroMovement(quantity);
        if (Quantity + quantity < 0)
        {
            throw new InvalidOperationException("Stock quantity cannot be negative.");
        }

        Quantity += quantity;
    }

    private static void ValidateNonZeroMovement(int quantity)
    {
        if (quantity == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Stock movement quantity cannot be zero.");
        }
    }
}

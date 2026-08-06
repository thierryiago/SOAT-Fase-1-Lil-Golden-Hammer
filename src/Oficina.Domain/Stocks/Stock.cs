namespace Oficina.Domain.Stocks;

public sealed class Stock
{
    private Stock(Guid id, Guid partId, int qty, DateTimeOffset createDate)
    {
        Id = id;
        PartId = partId;
        Qty = qty;
        CreateDate = createDate;
    }

    public Guid Id { get; }
    public Guid PartId { get; }
    public int Qty { get; private set; }
    public DateTimeOffset CreateDate { get; }

    public static Stock Create(Guid partId, int qty)
    {
        Validate(partId, qty);
        return new Stock(Guid.NewGuid(), partId, qty, DateTimeOffset.UtcNow);
    }

    public void UpdateQuantity(int qty)
    {
        ValidateQuantity(qty);
        Qty = qty;
    }

    public void AddQuantity(int qty)
    {
        ValidateMovement(qty);
        Qty += qty;
    }

    public void RemoveQuantity(int qty)
    {
        ValidateMovement(qty);

        if (Qty - qty < 0)
        {
            throw new InvalidOperationException("Stock quantity cannot be negative.");
        }

        Qty -= qty;
    }

    public void AdjustQuantity(int qty)
    {
        ValidateMovement(qty);

        if (Qty + qty < 0)
        {
            throw new InvalidOperationException("Stock quantity cannot be negative.");
        }

        Qty += qty;
    }

    private static void Validate(Guid partId, int qty)
    {
        if (partId == Guid.Empty)
        {
            throw new ArgumentException("Part reference is required.", nameof(partId));
        }

        ValidateQuantity(qty);
    }

    private static void ValidateQuantity(int qty)
    {
        if (qty < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(qty), "Stock quantity cannot be negative.");
        }
    }

    private static void ValidateMovement(int qty)
    {
        if (qty == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(qty), "Stock movement quantity cannot be zero.");
        }
    }
}

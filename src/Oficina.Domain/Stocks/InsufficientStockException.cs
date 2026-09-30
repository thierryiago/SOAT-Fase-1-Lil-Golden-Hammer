namespace Oficina.Domain.Stock;

public sealed class InsufficientStockException : Exception
{
    public InsufficientStockException(Guid partId, int available, int requested)
        : base($"Insufficient stock for part '{partId}'. Available: {available}, requested: {requested}.")
    {
        PartId = partId;
        Available = available;
        Requested = requested;
    }

    public Guid PartId { get; }
    public int Available { get; }
    public int Requested { get; }
}

namespace Oficina.Domain.ServiceOrders;

public sealed class ServiceOrderItem
{
    public ServiceOrderItem(Guid partId, string partName, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        PartId = partId;
        PartName = partName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid PartId { get; }
    public string PartName { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; }
    public decimal Total => Quantity * UnitPrice;
}

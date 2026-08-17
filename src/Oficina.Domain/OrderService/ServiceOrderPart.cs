using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Domain.OrderService
{
    public sealed class ServiceOrderPart
    {
        private ServiceOrderPart(Guid id, Guid partId, Guid orderServiceId, int quantityUsed)
        {
            Id = id;
            PartId = partId;
            OrderServiceId = orderServiceId;
            QuantityUsed = quantityUsed;
        }

        public Guid Id { get; }
        public Guid PartId { get; }
        public Guid OrderServiceId { get; }
        public int QuantityUsed { get; }
        public ServiceOrder? OrderService { get; set; }
        public Part? Part { get; set; }

        public static ServiceOrderPart Create(Guid partId, Guid orderServiceId, int quantityUsed)
        {
            if (quantityUsed <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantityUsed), "Quantity must be greater than zero.");
            }

            return new ServiceOrderPart(Guid.NewGuid(), partId, orderServiceId, quantityUsed);
        }
    }
}

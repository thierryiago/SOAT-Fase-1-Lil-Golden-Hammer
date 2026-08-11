using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Domain.OrderService
{
    public record ServiceOrderPart(Guid Id, Guid PartId, Guid OrderServiceId, int QuantityUsed)
    {
        public ServiceOrder? OrderService { get; set; }
        public Part? Part { get; set; }
    }
}

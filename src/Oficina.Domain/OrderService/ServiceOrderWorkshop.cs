using Oficina.Domain.ServiceOrders;
using Oficina.Domain.Services;

namespace Oficina.Domain.OrderService
{
    public sealed class ServiceOrderWorkshop
    {
        private ServiceOrderWorkshop(Guid id, Guid serviceOrderId, Guid workshopServiceId)
        {
            Id = id;
            ServiceOrderId = serviceOrderId;
            WorkshopServiceId = workshopServiceId;
        }

        public Guid Id { get; }
        public Guid ServiceOrderId { get; }
        public Guid WorkshopServiceId { get; }
        public ServiceOrder? ServiceOrder { get; set; }
        public WorkshopService? WorkshopService { get; set; }

        public static ServiceOrderWorkshop Create(Guid serviceOrderId, Guid workshopServiceId) =>
            new(Guid.NewGuid(), serviceOrderId, workshopServiceId);
    }
}

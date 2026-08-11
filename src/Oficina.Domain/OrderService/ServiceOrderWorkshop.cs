using Oficina.Domain.ServiceOrders;
using Oficina.Domain.Services;

namespace Oficina.Domain.OrderService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="ServiceOrderId"></param>
    /// <param name="WorkshopServiceId"></param>
    /// <param name="ServiceOrder"></param>
    /// <param name="WorkshopService"></param>
    public record ServiceOrderWorkshop(Guid Id, Guid ServiceOrderId, Guid WorkshopServiceId)
    {
        public ServiceOrder? ServiceOrder { get; set; }
        public WorkshopService? WorkshopService { get; set; }
    }
}

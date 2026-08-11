using Oficina.Domain.ServiceOrders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Oficina.Domain.OrderServiceHistory
{
    public record ServiceOrderHistory(Guid Id, Guid OrderServiceId, string? StatusName, DateTime CreatedDate)
    {
        public ServiceOrder? OrderService { get; set; }
    }
}

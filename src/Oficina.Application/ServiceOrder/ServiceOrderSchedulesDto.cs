namespace Oficina.Application.ServiceOrders;

public class ServiceOrderSchedulesDto
{
    public Guid OrderServiceId { get; set; }
    public DateTimeOffset ScheduleDate { get; set; }
}

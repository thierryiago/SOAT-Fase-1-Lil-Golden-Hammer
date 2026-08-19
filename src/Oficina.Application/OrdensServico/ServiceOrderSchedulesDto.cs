namespace Oficina.Application.OrdensServico
{
    public class ServiceOrderSchedulesDto
    {
        public Guid OrderServiceId { get; set; }
        public DateTimeOffset ScheduleDate { get; set; }
    }
}

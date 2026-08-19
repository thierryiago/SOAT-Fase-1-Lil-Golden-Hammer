using System;
using System.Collections.Generic;
using System.Text;

namespace Oficina.Application.OrdensServico
{
    public class ServiceOrderSchedulesDto
    {
        public Guid OrderServiceId { get; set; }
        public DateTimeOffset ScheduleDate { get; set; }
    }
}

using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Common;
using Oficina.Application.Customers;
using Oficina.Application.OrdensServico;
using Oficina.Application.ServiceOrders;

namespace Oficina.Api.Controllers;


[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
[Route("api/v1/schedules")]
public class ScheduleController : ControllerBase
{
    private readonly ServiceOrderService _serviceOrderService;

    public ScheduleController(ServiceOrderService serviceOrderService)
    {
        _serviceOrderService = serviceOrderService;
    }

    [HttpGet(Name = "ListSchedules")]
    [ProducesResponseType(typeof(PagedResponse<CustomerResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSchedules(DateTime? date)
    {
        List<ServiceOrderSchedulesDto> schedules;

        if (date.HasValue)
        {
            schedules = await _serviceOrderService.ListSchedulesByDateAsync(date.Value);
        }
        else
        {
            schedules = await _serviceOrderService.ListSchedulesAsync();
        }

        if (schedules.Count > 0)
        {
            return Ok(schedules);
        }

        return NotFound("No schedules found.");
    }
}

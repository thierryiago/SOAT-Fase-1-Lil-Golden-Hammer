using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.ServiceOrders;
using Oficina.Application.ServiceOrders.UseCases.Queries;
using System.Diagnostics.CodeAnalysis;

namespace Oficina.Api.Controllers;


[ApiController]
[Authorize]
[Route("api/v1/schedules")]
[ExcludeFromCodeCoverage]
public class ScheduleController : ControllerBase
{
    private readonly ListSchedulesUseCase _listSchedules;
    private readonly ListSchedulesByDateUseCase _listSchedulesByDate;

    public ScheduleController(
        ListSchedulesUseCase listSchedules,
        ListSchedulesByDateUseCase listSchedulesByDate)
    {
        _listSchedules = listSchedules;
        _listSchedulesByDate = listSchedulesByDate;
    }

    [HttpGet(Name = "ListSchedules")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ServiceOrderSchedulesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListSchedules(DateTime? date, CancellationToken cancellationToken)
    {
        List<ServiceOrderSchedulesDto> schedules;

        if (date.HasValue)
        {
            schedules = await _listSchedulesByDate.ExecuteAsync(date.Value, cancellationToken);
        }
        else
        {
            schedules = await _listSchedules.ExecuteAsync(cancellationToken);
        }

        if (schedules.Count > 0)
        {
            return Ok(schedules);
        }

        return NotFound("No schedules found.");
    }
}

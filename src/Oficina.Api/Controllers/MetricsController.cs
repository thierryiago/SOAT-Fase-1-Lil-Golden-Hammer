using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Metrics;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/v1/metrics")]
public sealed class MetricsController(MetricsService metrics) : ControllerBase
{
    private readonly MetricsService _metrics = metrics;

    [HttpGet("workshop-service/execution-time", Name = "GetWorkshopServiceExecutionTimes")]
    [ProducesResponseType(typeof(IReadOnlyCollection<WorkshopServiceExecutionTimeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkshopServiceExecutionTimes(
        CancellationToken cancellationToken)
    {
        var executionTimes = await _metrics.GetWorkshopServiceExecutionTimesAsync(cancellationToken);
        return Ok(executionTimes);
    }
}

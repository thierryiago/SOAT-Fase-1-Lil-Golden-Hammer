using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Metrics;
using Oficina.Application.Metrics.UseCases.Queries;
using System.Diagnostics.CodeAnalysis;

namespace Oficina.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/metrics")]
[ExcludeFromCodeCoverage]
public sealed class MetricsController(
    GetWorkshopServiceExecutionTimesUseCase getWorkshopServiceExecutionTimes) : ControllerBase
{
    private readonly GetWorkshopServiceExecutionTimesUseCase _getWorkshopServiceExecutionTimes = getWorkshopServiceExecutionTimes;

    [HttpGet("workshop-service/execution-time", Name = "GetWorkshopServiceExecutionTimes")]
    [ProducesResponseType(typeof(IReadOnlyCollection<WorkshopServiceExecutionTimeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkshopServiceExecutionTimes(
        CancellationToken cancellationToken)
    {
        var executionTimes = await _getWorkshopServiceExecutionTimes.GetWorkshopServiceExecutionTimesAsync(cancellationToken);
        return Ok(executionTimes);
    }
}

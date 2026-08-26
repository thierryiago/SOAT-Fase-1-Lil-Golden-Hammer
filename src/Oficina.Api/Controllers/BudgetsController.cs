using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Budgets;
using Oficina.Application.Common;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/v1/budgets")]
[ExcludeFromCodeCoverage]
public sealed class BudgetsController : ControllerBase
{
    private readonly BudgetService _budgets;

    public BudgetsController(BudgetService budgets)
    {
        _budgets = budgets;
    }

    [HttpGet(Name = "ListBudgets")]
    [ProducesResponseType(typeof(PagedResponse<BudgetResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        var budgets = await _budgets.ListAsync(request, cancellationToken);
        return Ok(budgets);
    }

    [HttpGet("{id:guid}", Name = "GetBudgetById")]
    [ProducesResponseType(typeof(BudgetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var budget = await _budgets.GetByIdAsync(id, cancellationToken);
        return budget is null ? NotFound() : Ok(budget);
    }
}

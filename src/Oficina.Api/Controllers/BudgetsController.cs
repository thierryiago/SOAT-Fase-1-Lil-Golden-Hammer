using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Budgets;
using Oficina.Application.Budgets.UseCases.Queries;
using Oficina.Application.Common;
using System.Diagnostics.CodeAnalysis;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/v1/budgets")]
[Authorize]
[ExcludeFromCodeCoverage]
public sealed class BudgetsController : ControllerBase
{
    private readonly ListBudgetsUseCase _listBudgets;
    private readonly GetBudgetByIdUseCase _getBudgetById;

    public BudgetsController(ListBudgetsUseCase listBudgets, GetBudgetByIdUseCase getBudgetById)
    {
        _listBudgets = listBudgets;
        _getBudgetById = getBudgetById;
    }

    [HttpGet(Name = "ListBudgets")]
    [ProducesResponseType(typeof(PagedResponse<BudgetResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        var budgets = await _listBudgets.ListAsync(request, cancellationToken);
        return Ok(budgets);
    }

    [HttpGet("{id:guid}", Name = "GetBudgetById")]
    [ProducesResponseType(typeof(BudgetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var budget = await _getBudgetById.GetByIdAsync(id, cancellationToken);
        return budget is null ? NotFound() : Ok(budget);
    }
}

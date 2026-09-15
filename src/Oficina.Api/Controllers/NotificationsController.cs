using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Budgets;
using Oficina.Application.Notifications;
using Oficina.Application.ServiceOrders;
using System.Diagnostics.CodeAnalysis;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[ExcludeFromCodeCoverage]
public sealed class NotificationsController(NotificationService notifications, IBudgetService budgetService, ServiceOrderService serviceOrderService) : ControllerBase
{
    private readonly NotificationService _notifications = notifications;
    private readonly IBudgetService _budgetService = budgetService;
    private readonly ServiceOrderService _serviceOrderService = serviceOrderService;

    [HttpPost("email", Name = "SendEmailNotification")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendEmail(
        SendEmailNotificationRequest request,
        CancellationToken cancellationToken)
    {
        await _notifications.SendEmailAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpGet("approveBudget", Name = "ApproveBudget")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendApproveBudget(
        string budgetId, CancellationToken cancellationToken)
    {
        var budget = await _budgetService.SetApprovalByBudgetIdAsync(Guid.Parse(budgetId), true, cancellationToken);
        await _serviceOrderService.ApproveAsync(budget.ServiceOrderId, cancellationToken);
        return Ok("Orçamento aprovado");
    }

    [HttpGet("rejectBudget", Name = "RejectBudget")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendRejectBudget(
        string budgetId, CancellationToken cancellationToken)
    {
        var budget = await _budgetService.SetApprovalByBudgetIdAsync(Guid.Parse(budgetId), false, cancellationToken);
        await _serviceOrderService.CancelAsync(budget.ServiceOrderId, cancellationToken);
        return Ok("Orçamento rejeitado");
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Budgets;
using Oficina.Application.Notifications;
using Oficina.Application.ServiceOrders.UseCases;
using System.Diagnostics.CodeAnalysis;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[ExcludeFromCodeCoverage]
public sealed class NotificationsController(
    INotificationEmailSender notificationEmailSender,
    IBudgetService budgetService,
    ApproveServiceOrderUseCase approveServiceOrder,
    CancelServiceOrderUseCase cancelServiceOrder) : ControllerBase
{
    private readonly INotificationEmailSender _notificationEmailSender = notificationEmailSender;
    private readonly IBudgetService _budgetService = budgetService;
    private readonly ApproveServiceOrderUseCase _approveServiceOrder = approveServiceOrder;
    private readonly CancelServiceOrderUseCase _cancelServiceOrder = cancelServiceOrder;

    [HttpPost("email", Name = "SendEmailNotification")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendEmail(
        SendEmailNotificationRequest request,
        CancellationToken cancellationToken)
    {
        await _notificationEmailSender.SendEmailAsync(request, cancellationToken);
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
        await _approveServiceOrder.ExecuteAsync(budget.ServiceOrderId, cancellationToken);
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
        await _cancelServiceOrder.ExecuteAsync(budget.ServiceOrderId, cancellationToken);
        return Ok("Orçamento rejeitado");
    }
}

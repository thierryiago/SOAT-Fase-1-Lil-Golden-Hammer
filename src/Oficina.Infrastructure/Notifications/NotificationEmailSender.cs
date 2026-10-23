using Oficina.Application.Budgets;
using Oficina.Application.Notifications;

namespace Oficina.Infrastructure.Notifications;

public sealed class NotificationEmailSender(
    SendEmailNotification sendEmailNotification,
    SendBudgetAwaitingApproval sendBudgetAwaitingApproval,
    SendVehicleReadyForPickup sendVehicleReadyForPickup,
    SendServiceOrderStatus sendServiceOrderStatus,
    SendServiceOrderOpenedAwaitingApproval? sendServiceOrderOpened = null) : INotificationEmailSender
{
    private readonly SendEmailNotification _sendEmailNotification = sendEmailNotification;
    private readonly SendBudgetAwaitingApproval _sendBudgetAwaitingApproval = sendBudgetAwaitingApproval;
    private readonly SendVehicleReadyForPickup _sendVehicleReadyForPickup = sendVehicleReadyForPickup;

    public Task SendServiceOrderStatusAsync(ServiceOrderStatusEmail context, CancellationToken cancellationToken) =>
        sendServiceOrderStatus.SendAsync(context, cancellationToken);

    public Task SendEmailAsync(SendEmailNotificationRequest request, CancellationToken cancellationToken) =>
        _sendEmailNotification.SendEmailAsync(request, cancellationToken);

    public Task SendBudgetAwaitingApprovalAsync(
        string customerName,
        string customerEmail,
        BudgetResponse budget,
        CancellationToken cancellationToken, ServiceOrderStatusEmail? context = null) =>
        _sendBudgetAwaitingApproval.SendBudgetAwaitingApprovalAsync(customerName, customerEmail, budget, cancellationToken, context);

    public Task SendVehicleReadyForPickupAsync(
        string customerName,
        string customerEmail,
        string vehiclePlate,
        string vehicleBrand,
        string vehicleModel,
        int vehicleYear,
        CancellationToken cancellationToken, ServiceOrderStatusEmail? context = null) =>
        _sendVehicleReadyForPickup.SendVehicleReadyForPickupAsync(
            customerName,
            customerEmail,
            vehiclePlate,
            vehicleBrand,
            vehicleModel,
            vehicleYear,
            cancellationToken, context);

    public Task SendServiceOrderOpenedAsync(ServiceOrderOpenedEmail email, CancellationToken cancellationToken) =>
        (sendServiceOrderOpened ?? throw new InvalidOperationException("Service order opened email sender is not configured."))
            .SendAsync(email, cancellationToken);
}

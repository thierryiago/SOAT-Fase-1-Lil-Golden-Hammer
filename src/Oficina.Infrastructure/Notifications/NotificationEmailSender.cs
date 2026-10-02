using Oficina.Application.Budgets;
using Oficina.Application.Notifications;

namespace Oficina.Infrastructure.Notifications;

public sealed class NotificationEmailSender(
    SendEmailNotification sendEmailNotification,
    SendBudgetAwaitingApproval sendBudgetAwaitingApproval,
    SendVehicleReadyForPickup sendVehicleReadyForPickup) : INotificationEmailSender
{
    private readonly SendEmailNotification _sendEmailNotification = sendEmailNotification;
    private readonly SendBudgetAwaitingApproval _sendBudgetAwaitingApproval = sendBudgetAwaitingApproval;
    private readonly SendVehicleReadyForPickup _sendVehicleReadyForPickup = sendVehicleReadyForPickup;

    public Task SendEmailAsync(SendEmailNotificationRequest request, CancellationToken cancellationToken) =>
        _sendEmailNotification.SendEmailAsync(request, cancellationToken);

    public Task SendBudgetAwaitingApprovalAsync(
        string customerName,
        string customerEmail,
        BudgetResponse budget,
        CancellationToken cancellationToken) =>
        _sendBudgetAwaitingApproval.SendBudgetAwaitingApprovalAsync(customerName, customerEmail, budget, cancellationToken);

    public Task SendVehicleReadyForPickupAsync(
        string customerName,
        string customerEmail,
        string vehiclePlate,
        string vehicleBrand,
        string vehicleModel,
        int vehicleYear,
        CancellationToken cancellationToken) =>
        _sendVehicleReadyForPickup.SendVehicleReadyForPickupAsync(
            customerName,
            customerEmail,
            vehiclePlate,
            vehicleBrand,
            vehicleModel,
            vehicleYear,
            cancellationToken);
}

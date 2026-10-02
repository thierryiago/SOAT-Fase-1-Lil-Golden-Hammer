using Oficina.Application.Budgets;

namespace Oficina.Application.Notifications;

public interface INotificationEmailSender
{
    Task SendEmailAsync(SendEmailNotificationRequest request, CancellationToken cancellationToken);

    Task SendBudgetAwaitingApprovalAsync(
        string customerName,
        string customerEmail,
        BudgetResponse budget,
        CancellationToken cancellationToken);

    Task SendVehicleReadyForPickupAsync(
        string customerName,
        string customerEmail,
        string vehiclePlate,
        string vehicleBrand,
        string vehicleModel,
        int vehicleYear,
        CancellationToken cancellationToken);
}

using Oficina.Application.Budgets;

namespace Oficina.Application.Notifications;

public interface INotificationEmailSender
{
    Task SendServiceOrderStatusAsync(ServiceOrderStatusEmail context, CancellationToken cancellationToken);

    Task SendEmailAsync(SendEmailNotificationRequest request, CancellationToken cancellationToken);

    Task SendBudgetAwaitingApprovalAsync(
        string customerName,
        string customerEmail,
        BudgetResponse budget,
        CancellationToken cancellationToken, ServiceOrderStatusEmail? context = null);

    Task SendVehicleReadyForPickupAsync(
        string customerName,
        string customerEmail,
        string vehiclePlate,
        string vehicleBrand,
        string vehicleModel,
        int vehicleYear,
        CancellationToken cancellationToken, ServiceOrderStatusEmail? context = null);
}

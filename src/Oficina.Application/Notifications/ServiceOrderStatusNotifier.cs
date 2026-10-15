using Microsoft.Extensions.Logging;
using Oficina.Application.Budgets;
using Oficina.Application.Customers;
using Oficina.Application.Vehicles;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Application.Notifications;

public sealed class ServiceOrderStatusNotifier(
    ICustomerRepository customers,
    IVehicleRepository vehicles,
    INotificationEmailSender sender,
    ILogger<ServiceOrderStatusNotifier> logger)
{
    public async Task NotifyAsync(ServiceOrder order, ServiceOrderStatus? previousStatus,
        CancellationToken cancellationToken, BudgetResponse? budget = null)
    {
        if (order.Status == previousStatus)
            return;

        try
        {
            var customer = await customers.GetByIdAsync(order.CustomerId, cancellationToken)
                ?? throw new InvalidOperationException("Notification customer was not found.");
            var vehicle = order.VehicleId is { } vehicleId
                ? await vehicles.GetByIdAsync(vehicleId, cancellationToken)
                : null;
            if (vehicle is null)
                throw new InvalidOperationException("Notification vehicle was not found.");

            var context = new ServiceOrderStatusEmail(order.Id, customer.Name, customer.Email,
                vehicle.Plate, vehicle.Brand, vehicle.Model, vehicle.Year, previousStatus,
                order.Status ?? throw new InvalidOperationException("Notification status is missing."));

            switch (order.Status)
            {
                case ServiceOrderStatus.AwaitingApproval:
                    await sender.SendBudgetAwaitingApprovalAsync(customer.Name, customer.Email,
                        budget ?? throw new InvalidOperationException("Notification budget is missing."),
                        cancellationToken, context);
                    break;
                case ServiceOrderStatus.Finalized:
                    await sender.SendVehicleReadyForPickupAsync(customer.Name, customer.Email,
                        vehicle.Plate, vehicle.Brand, vehicle.Model, vehicle.Year, cancellationToken, context);
                    break;
                default:
                    await sender.SendServiceOrderStatusAsync(context, cancellationToken);
                    break;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // SMTP exceptions can contain recipients; log only operational metadata.
            logger.LogError("Service order email failed. OrderId: {OrderId}, PreviousStatus: {PreviousStatus}, Status: {Status}, FailureType: {FailureType}",
                order.Id, previousStatus, order.Status, exception.GetType().Name);
        }
    }
}

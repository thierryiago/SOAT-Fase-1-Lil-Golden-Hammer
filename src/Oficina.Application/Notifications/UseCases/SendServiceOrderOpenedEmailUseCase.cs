using Microsoft.Extensions.Logging;
using Oficina.Application.Budgets;
using Oficina.Domain.Customers;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.Vehicles;

namespace Oficina.Application.Notifications.UseCases;

public sealed class SendServiceOrderOpenedEmailUseCase(
    INotificationEmailSender sender,
    ILogger<SendServiceOrderOpenedEmailUseCase> logger)
{
    public async Task ExecuteAsync(
        ServiceOrder serviceOrder,
        Customer customer,
        Vehicle vehicle,
        BudgetResponse budget,
        CancellationToken cancellationToken)
    {
        try
        {
            var email = new ServiceOrderOpenedEmail(
                serviceOrder.Id,
                serviceOrder.Description,
                serviceOrder.CreatedAt,
                customer.Name,
                customer.Document,
                customer.Email,
                customer.TelephoneNumber,
                vehicle.Plate,
                vehicle.Brand,
                vehicle.Model,
                vehicle.Year,
                budget);

            await sender.SendServiceOrderOpenedAsync(email, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError("Service order opened email failed. OrderId: {OrderId}, FailureType: {FailureType}",
                serviceOrder.Id, exception.GetType().Name);
        }
    }
}

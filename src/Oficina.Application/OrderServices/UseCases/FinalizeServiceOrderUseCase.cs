using Oficina.Application.Customers;
using Oficina.Application.Notifications;
using Oficina.Application.OrderServiceHistory;
using Oficina.Application.Vehicles;
using Oficina.Domain.OrderService;
using Oficina.Domain.OrderServiceHistory;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Application.ServiceOrders.UseCases;

public sealed class FinalizeServiceOrderUseCase(
    IServiceOrderRepository serviceOrders,
    IServiceOrderHistoryRepository history,
    ICustomerRepository customers,
    IVehicleRepository vehicles,
    INotificationEmailSender notificationEmailSender)
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrders;
    private readonly IServiceOrderHistoryRepository _history = history;
    private readonly ICustomerRepository _customerRepository = customers;
    private readonly IVehicleRepository _vehicleRepository = vehicles;
    private readonly INotificationEmailSender _notificationEmailSender = notificationEmailSender;

    public async Task<ServiceOrderDetailResponse> ExecuteAsync(Guid serviceOrderId, CancellationToken cancellationToken)
    {
        var serviceOrder = await _serviceOrderRepository.GetByIdAsync(serviceOrderId, cancellationToken);
        if (serviceOrder is null)
        {
            throw new InvalidOperationException("Service Order was not found!");
        }

        if (serviceOrder.Status != ServiceOrderStatus.InExecution)
        {
            throw new InvalidOperationException(
                $"Invalid service order for this action. Current status: {serviceOrder.Status?.ToString() ?? "None"}.");
        }

        var previousStatus = serviceOrder.Status;
        serviceOrder.UpdateStatus(finalized: true);

        await _serviceOrderRepository.UpdateAsync(
            serviceOrder,
            Array.Empty<ServiceOrderPart>(),
            Array.Empty<ServiceOrderWorkshop>(),
            cancellationToken);
        await RecordHistoryAsync(serviceOrder, previousStatus, cancellationToken);

        var customer = await _customerRepository.GetByIdAsync(serviceOrder.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException("Customer was not found.");
        var vehicle = serviceOrder.VehicleId is { } vehicleId
            ? await _vehicleRepository.GetByIdAsync(vehicleId, cancellationToken)
            : null;
        if (vehicle is null)
        {
            throw new InvalidOperationException("Vehicle was not found.");
        }

        await _notificationEmailSender.SendVehicleReadyForPickupAsync(
            customer.Name,
            customer.Email,
            vehicle.Plate,
            vehicle.Brand,
            vehicle.Model,
            vehicle.Year,
            cancellationToken);

        return ServiceOrderResponseMapper.MapDetail(serviceOrder);
    }

    private async Task RecordHistoryAsync(
        ServiceOrder serviceOrder,
        ServiceOrderStatus? previousStatus,
        CancellationToken cancellationToken)
    {
        if (serviceOrder.Status == previousStatus)
        {
            return;
        }

        var history = ServiceOrderHistory.Create(serviceOrder.Id, serviceOrder.Status?.ToString());
        await _history.AddAsync(history, cancellationToken);
    }
}

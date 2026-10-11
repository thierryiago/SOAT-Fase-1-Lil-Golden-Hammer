using Oficina.Application.Customers;
using Oficina.Application.Vehicles;
using Oficina.Domain.ServiceOrders;

namespace Oficina.Application.ServiceOrders.UseCases;

public sealed class OpenServiceOrderUseCase(
    IServiceOrderRepository serviceOrders,
    ICustomerRepository customers,
    IVehicleRepository vehicles)
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrders;
    private readonly ICustomerRepository _customerRepository = customers;
    private readonly IVehicleRepository _vehicleRepository = vehicles;

    public async Task<ServiceOrderDetailResponse> ExecuteAsync(
        OpenServiceOrderRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
            throw new InvalidOperationException("Customer was not found.");

        var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken);
        if (vehicle is null)
            throw new InvalidOperationException("Vehicle was not found.");

        var serviceOrder = ServiceOrder.Open(request.CustomerId, request.VehicleId, request.Description);
        await _serviceOrderRepository.AddAsync(serviceOrder, cancellationToken);
        return ServiceOrderResponseMapper.MapDetail(serviceOrder);
    }
}

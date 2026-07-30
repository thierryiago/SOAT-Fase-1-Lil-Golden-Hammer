using Oficina.Application.Customers;

namespace Oficina.Application.Vehicles;

public sealed class VehicleService
{
    private readonly ICustomerRepository _customers;

    public VehicleService(ICustomerRepository customers)
    {
        _customers = customers;
    }

    public async Task<CustomerVehicleRegistrationResponse> IdentifyCustomerAndRegisterVehicleAsync(
        IdentifyCustomerAndRegisterVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByDocumentAsync(request.Document, cancellationToken);
        if (customer is null)
        {
            throw new InvalidOperationException("Customer document was not found.");
        }

        var vehicle = customer.RegisterVehicle(request.Plate, request.Make, request.Model, request.Year);
        await _customers.UpdateAsync(customer, cancellationToken);

        return new CustomerVehicleRegistrationResponse(
            customer.Id,
            customer.Name,
            customer.Document,
            new VehicleResponse(vehicle.Plate, vehicle.Make, vehicle.Model, vehicle.Year));
    }
}

using Oficina.Application.Customers;

namespace Oficina.Application.Vehicles.UseCases;

public class IdentifyCustomerAndRegisterVehicleUseCase(
    ICustomerRepository customers,
    CreateVehicleUseCase createVehicle)
{
    private readonly ICustomerRepository _customers = customers;
    private readonly CreateVehicleUseCase _createVehicle = createVehicle;

    public async Task<CustomerVehicleRegistrationResponse> IdentifyCustomerAndRegisterVehicleAsync(
        IdentifyCustomerAndRegisterVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByDocumentAsync(request.Document, cancellationToken);
        if (customer is null || !customer.IsActive)
        {
            throw new KeyNotFoundException("Customer document was not found.");
        }

        var vehicle = await _createVehicle.CreateAsync(
            new CreateVehicleRequest(
                customer.Id,
                request.Plate,
                request.Brand,
                request.Model,
                request.Year),
            cancellationToken);

        return new CustomerVehicleRegistrationResponse(
            customer.Id,
            customer.Name,
            customer.Document,
            vehicle);
    }
}

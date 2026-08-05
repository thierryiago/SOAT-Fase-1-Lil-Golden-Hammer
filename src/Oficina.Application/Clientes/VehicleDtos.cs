namespace Oficina.Application.Vehicles;

public sealed record CreateVehicleRequest(
    Guid CustomerId,
    string Plate,
    string Make,
    string Model,
    int Year);

public sealed record UpdateVehicleRequest(
    string Plate,
    string Make,
    string Model,
    int Year);

public sealed record IdentifyCustomerAndRegisterVehicleRequest(
    string Document,
    string Plate,
    string Make,
    string Model,
    int Year);

public sealed record CustomerVehicleRegistrationResponse(
    Guid CustomerId,
    string Name,
    string Document,
    VehicleResponse Vehicle);

public sealed record VehicleResponse(
    Guid Id,
    Guid CustomerId,
    string Plate,
    string Make,
    string Model,
    int Year);

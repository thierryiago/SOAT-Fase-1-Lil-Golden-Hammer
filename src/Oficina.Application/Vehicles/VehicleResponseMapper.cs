using Oficina.Domain.Customers;
using Oficina.Domain.Vehicles;

namespace Oficina.Application.Vehicles;

internal static class VehicleResponseMapper
{
    public static VehicleResponse Map(Vehicle vehicle) =>
        new(
            vehicle.Id,
            vehicle.CustomerId,
            vehicle.Plate,
            vehicle.Brand,
            vehicle.Model,
            vehicle.Year);
}

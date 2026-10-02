using Oficina.Application.Common;

namespace Oficina.Application.Vehicles.UseCases.Queries;

public class ListVehiclesUseCase(
    IVehicleRepository vehicles)
{
    private readonly IVehicleRepository _vehicles = vehicles;

    public async Task<PagedResponse<VehicleResponse>> ListAsync(
        PageRequest request,
        Guid? customerId,
        CancellationToken cancellationToken)
    {
        var vehicles = await _vehicles.ListAsync(cancellationToken);
        var search = request.Search?.Trim();
        var query = vehicles
            .Where(vehicle => vehicle.IsActive)
            .Where(vehicle => customerId is null || vehicle.CustomerId == customerId)
            .Where(vehicle =>
                string.IsNullOrWhiteSpace(search) ||
                vehicle.Plate.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                vehicle.Brand.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                vehicle.Model.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(vehicle => vehicle.Plate)
            .Select(VehicleResponseMapper.Map);

        return Pagination.Create(query, request);
    }
}

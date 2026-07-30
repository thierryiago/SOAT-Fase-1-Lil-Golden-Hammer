using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Vehicles;

namespace Oficina.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
public sealed class VehiclesController : ControllerBase
{
    private readonly VehicleService _vehicles;

    public VehiclesController(VehicleService vehicles)
    {
        _vehicles = vehicles;
    }

    [HttpPost("identify-customer-and-register", Name = "IdentifyCustomerAndRegisterVehicle")]
    [ProducesResponseType(typeof(CustomerVehicleRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IdentifyCustomerAndRegister(
        IdentifyCustomerAndRegisterVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _vehicles.IdentifyCustomerAndRegisterVehicleAsync(request, cancellationToken);
        return Ok(response);
    }
}

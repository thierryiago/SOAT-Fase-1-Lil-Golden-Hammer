using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Common;
using Oficina.Application.Vehicles;
using Oficina.Application.Vehicles.UseCases;
using Oficina.Application.Vehicles.UseCases.Queries;
using System.Diagnostics.CodeAnalysis;

namespace Oficina.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/vehicles")]
[ExcludeFromCodeCoverage]
public sealed class VehiclesController : ControllerBase
{
    private readonly CreateVehicleUseCase _createVehicle;
    private readonly IdentifyCustomerAndRegisterVehicleUseCase _identifyCustomerAndRegisterVehicle;
    private readonly UpdateVehicleUseCase _updateVehicle;
    private readonly DeleteVehicleUseCase _deleteVehicle;
    private readonly ListVehiclesUseCase _listVehicles;
    private readonly GetVehicleByIdUseCase _getVehicleById;

    public VehiclesController(
        CreateVehicleUseCase createVehicle,
        IdentifyCustomerAndRegisterVehicleUseCase identifyCustomerAndRegisterVehicle,
        UpdateVehicleUseCase updateVehicle,
        DeleteVehicleUseCase deleteVehicle,
        ListVehiclesUseCase listVehicles,
        GetVehicleByIdUseCase getVehicleById)
    {
        _createVehicle = createVehicle;
        _identifyCustomerAndRegisterVehicle = identifyCustomerAndRegisterVehicle;
        _updateVehicle = updateVehicle;
        _deleteVehicle = deleteVehicle;
        _listVehicles = listVehicles;
        _getVehicleById = getVehicleById;
    }

    [HttpGet(Name = "ListVehicles")]
    [ProducesResponseType(typeof(PagedResponse<VehicleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] PageRequest request,
        [FromQuery] Guid? customerId,
        CancellationToken cancellationToken)
    {
        var vehicles = await _listVehicles.ListAsync(request, customerId, cancellationToken);
        return Ok(vehicles);
    }

    [HttpGet("{id:guid}", Name = "GetVehicleById")]
    [ProducesResponseType(typeof(VehicleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var vehicle = await _getVehicleById.GetByIdAsync(id, cancellationToken);
        return vehicle is null ? NotFound() : Ok(vehicle);
    }

    [HttpPost(Name = "CreateVehicle")]
    [ProducesResponseType(typeof(VehicleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        CreateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var vehicle = await _createVehicle.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, vehicle);
    }

    [HttpPost("identify-customer-and-register", Name = "IdentifyCustomerAndRegisterVehicle")]
    [ProducesResponseType(typeof(CustomerVehicleRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IdentifyCustomerAndRegister(
        IdentifyCustomerAndRegisterVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _identifyCustomerAndRegisterVehicle.IdentifyCustomerAndRegisterVehicleAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPut("{id:guid}", Name = "UpdateVehicle")]
    [ProducesResponseType(typeof(VehicleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var vehicle = await _updateVehicle.UpdateAsync(id, request, cancellationToken);
        return Ok(vehicle);
    }

    [HttpDelete("{id:guid}", Name = "DeleteVehicle")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _deleteVehicle.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}

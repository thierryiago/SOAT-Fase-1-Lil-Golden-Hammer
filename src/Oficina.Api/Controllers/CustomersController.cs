using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Application.Common;
using Oficina.Application.Customers;
using Oficina.Application.Customers.UseCases;
using Oficina.Application.Customers.UseCases.Queries;
using System.Diagnostics.CodeAnalysis;

namespace Oficina.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/customers")]
[ExcludeFromCodeCoverage]
public sealed class CustomersController : ControllerBase
{
    private readonly CreateCustomerUseCase _createCustomer;
    private readonly UpdateCustomerUseCase _updateCustomer;
    private readonly DeleteCustomerUseCase _deleteCustomer;
    private readonly ListCustomersUseCase _listCustomers;
    private readonly GetCustomerByIdUseCase _getCustomerById;

    public CustomersController(
        CreateCustomerUseCase createCustomer,
        UpdateCustomerUseCase updateCustomer,
        DeleteCustomerUseCase deleteCustomer,
        ListCustomersUseCase listCustomers,
        GetCustomerByIdUseCase getCustomerById)
    {
        _createCustomer = createCustomer;
        _updateCustomer = updateCustomer;
        _deleteCustomer = deleteCustomer;
        _listCustomers = listCustomers;
        _getCustomerById = getCustomerById;
    }

    [HttpGet(Name = "ListCustomers")]
    [ProducesResponseType(typeof(PagedResponse<CustomerResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        var customers = await _listCustomers.ListAsync(request, cancellationToken);
        return Ok(customers);
    }

    [HttpGet("{id:guid}", Name = "GetCustomerById")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _getCustomerById.GetByIdAsync(id, cancellationToken);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost(Name = "CreateCustomer")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await _createCustomer.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    [HttpPut("{id:guid}", Name = "UpdateCustomer")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await _updateCustomer.UpdateAsync(id, request, cancellationToken);
        return Ok(customer);
    }

    [HttpDelete("{id:guid}", Name = "DeleteCustomer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _deleteCustomer.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}

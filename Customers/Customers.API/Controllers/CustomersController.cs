using Customers.Application.Common.Interfaces;
using Customers.Application.Common.Models;
using Customers.Application.DTOs;
using Customers.Application.Features.Commands;
using Customers.Application.Features.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Customers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICustomerRepository _customerRepo;

    public CustomersController(IMediator mediator, ICustomerRepository customerRepo)
    {
        _mediator = mediator;
        _customerRepo = customerRepo;
    }

    [HttpPost]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
    public async Task<IActionResult> Create(CreateCustomerDto dto)
    {
        var id = await _mediator.Send(new CreateCustomerCommand(dto));
        return Ok(id);
    }

    [HttpPost("paged")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
    public async Task<IActionResult> GetCustomers([FromBody] GridRequest request)
    {
        var result = await _mediator.Send(new GetCustomersPagedQuery(request));
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetCustomersQuery());
        return Ok(result);
    }

    [HttpPost("get-names")]
    public async Task<IActionResult> GetCustomerNames([FromBody] List<Guid> ids)
    {
        if (ids == null || !ids.Any()) return BadRequest("No IDs provided");

        var names = await _customerRepo.GetCustomerNamesByIdsAsync(ids);
        return Ok(names);
    }

    [HttpPost("get-details")]
    public async Task<IActionResult> GetCustomerDetails([FromBody] List<Guid> ids)
    {
        if (ids == null || !ids.Any()) return BadRequest("No IDs provided");

        var details = await _customerRepo.GetCustomerDetailsByIdsAsync(ids);
        return Ok(details);
    }

    [HttpGet("{id}/name")]
    public async Task<IActionResult> GetCustomerNameById(Guid id)
    {
        var name = await _customerRepo.GetCustomerNameByIdAsync(id);

        if (string.IsNullOrEmpty(name))
        {
            return NotFound("Customer not found");
        }

        return Ok(name);
    }

    [HttpGet("lookup")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
    public async Task<IActionResult> GetCustomerLookup()
    {
        var customers = await _customerRepo.GetCustomersLookupAsync();
        return Ok(customers);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetCustomerByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateCustomerDto dto)
    {
        var result = await _mediator.Send(new UpdateCustomerCommand(id, dto));
        if (!result) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteCustomerCommand(id));
        if (!result) return NotFound();
        return Ok(result);
    }

    [HttpGet("search-ids")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
    public async Task<ActionResult<List<Guid>>> SearchIdsByName([FromQuery] string name)
    {
        var ids = await _customerRepo.GetIdsByNameAsync(name);
        return Ok(ids);
    }
}

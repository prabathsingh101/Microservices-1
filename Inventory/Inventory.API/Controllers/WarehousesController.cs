using Inventory.Application.Locations.Warehouses.Commands.CreateWarehouse;
using Inventory.Application.Locations.Warehouses.Commands.DeleteWarehouse;
using Inventory.Application.Locations.Warehouses.Commands.UpdateWarehouse;
using Inventory.Application.Locations.Warehouses.Queries.GetWarehouses;
using Inventory.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.API.Common;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/warehouses")]
public sealed class WarehousesController : ControllerBase
{
    private readonly IMediator _mediator;

    public WarehousesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse,Super Admin")]
    public async Task<IActionResult> Create(CreateWarehouseCommand command)
    {
        // 🚀 SMART INJECTION: Get CompanyId from Claims
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        if (Guid.TryParse(companyIdClaim, out var companyId))
        {
            command = command with { CompanyId = companyId };
        }

        var id = await _mediator.Send(command);
        return Ok(ApiResponse<Guid>.Ok(id, "Warehouse created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateWarehouseCommand command)
    {
        if (id != command.Id)
            return BadRequest(ApiResponse<string>.Fail("Id mismatch"));

        // 🚀 SMART INJECTION: Get CompanyId from Claims
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        if (Guid.TryParse(companyIdClaim, out var companyId))
        {
            command = command with { CompanyId = companyId };
        }

        await _mediator.Send(command);
        return Ok(ApiResponse<Guid>.Ok(id, "Warehouse updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteWarehouseCommand(id));
        return Ok(new { success = true, message = "Warehouse deleted successfully" });
    }

    [HttpGet]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetWarehousesQuery());
        return Ok(result);
    }
}

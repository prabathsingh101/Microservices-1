using Inventory.Application.Locations.Racks.Commands.CreateRack;
using Inventory.Application.Locations.Racks.Commands.UpdateRack;
using Inventory.Application.Locations.Racks.Commands.DeleteRack;
using Inventory.Application.Locations.Racks.Queries.GetRacks;
using Inventory.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.API.Common;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/racks")]
public sealed class RacksController : ControllerBase
{
    private readonly IMediator _mediator;

    public RacksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> Create(CreateRackCommand command)
    {
        // 🚀 SMART INJECTION: Get CompanyId from Claims
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        if (Guid.TryParse(companyIdClaim, out var companyId))
        {
            command = command with { CompanyId = companyId };
        }

        var id = await _mediator.Send(command);
        return Ok(ApiResponse<Guid>.Ok(id, "Rack created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateRackCommand command)
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
        return Ok(ApiResponse<Guid>.Ok(id, "Rack updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteRackCommand(id));
        return Ok(new { success = true, message = "Rack deleted successfully" });
    }

    [HttpGet]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetRacksQuery());
        return Ok(result);
    }
}

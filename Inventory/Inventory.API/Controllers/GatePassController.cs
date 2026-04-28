using Inventory.API.Common;
using Inventory.Application.Common.Models;
using Inventory.Application.GatePasses.Commands.CreateGatePass;
using Inventory.Application.GatePasses.Commands.DeleteGatePass;
using Inventory.Application.GatePasses.DTOs;
using Inventory.Application.GatePasses.Queries.CheckDuplicateGatePass;
using Inventory.Application.GatePasses.Queries.GetGatePassById;
using Inventory.Application.GatePasses.Queries.GetGatePassesPaged;
using Inventory.Application.GatePasses.Queries.GetVehicleAutocomplete;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Inventory.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GatePassController : ControllerBase
    {
        private readonly IMediator _mediator;

        public GatePassController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("Save")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Create(CreateGatePassCommand command)
        {
            try 
            {
                // 🔥 SMART INJECTION: Inject CompanyId & BranchId from Headers or Secure Claims
                var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
                command.CompanyId = Guid.TryParse(companyIdHeader, out var cId) ? cId : User.GetCompanyId();

                var branchIdClaim = User.GetBranchId();
                var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();
                command.BranchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
                    ? branchIdHeader 
                    : branchIdClaim;

                var result = await _mediator.Send(command);
                return Ok(ApiResponse<GatePassDto>.Ok(
                    result,
                    "Gate Pass generated successfully"
                ));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail($"Backend Error: {ex.Message} - {ex.InnerException?.Message}"));
            }
        }

        [HttpPost("GetPaged")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetPaged(GetGatePassesQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetGatePassByIdQuery(id));
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("CheckDuplicate")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> CheckDuplicate([FromQuery] string referenceNo, [FromQuery] string passType)
        {
            var result = await _mediator.Send(new CheckDuplicateGatePassQuery(referenceNo, passType));
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _mediator.Send(new DeleteGatePassCommand(id));
            if (!result) return NotFound();
            
            return Ok(ApiResponse<bool>.Ok(
                true,
                "Gate Pass deleted successfully"
            ));
        }

        [HttpGet("GetVehicleAutocomplete")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetVehicleAutocomplete([FromQuery] string searchTerm)
        {
            var result = await _mediator.Send(new GetVehicleAutocompleteQuery { SearchTerm = searchTerm });
            return Ok(result);
        }
    }
}

using Inventory.Application.DeliveryChallans.Commands;
using Inventory.Application.DeliveryChallans.DTOs;
using Inventory.Application.DeliveryChallans.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Inventory.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeliveryChallanController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DeliveryChallanController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("save")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> Save([FromBody] DeliveryChallanDto dto)
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
            
            if (Guid.TryParse(companyIdHeader, out var companyId) || Guid.TryParse(companyIdClaim, out companyId))
            {
                dto.CompanyId = companyId;
            }

            var branchIdClaim = User.FindFirst("BranchId")?.Value;
            var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();

            string? finalBranchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
                ? branchIdHeader 
                : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

            if (!string.IsNullOrEmpty(finalBranchId))
            {
                dto.BranchId = finalBranchId;
            }

            var result = await _mediator.Send(new CreateDeliveryChallanCommand(dto));
            return Ok(result);
        }

        [HttpGet("list")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> GetList()
        {
            var result = await _mediator.Send(new GetDeliveryChallanListQuery());
            return Ok(result);
        }

        [HttpGet("pending/{customerId}")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> GetPending(Guid customerId)
        {
            var result = await _mediator.Send(new GetPendingDeliveryChallansQuery(customerId));
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetDeliveryChallanByIdQuery(id));
            if (result == null) return NotFound(new { message = "Delivery Challan not found" });
            return Ok(result);
        }
    }
}

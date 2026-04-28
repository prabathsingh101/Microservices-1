using Inventory.Application.Common.Interfaces;
using Inventory.Application.GRN.Command;
using Inventory.Application.GRN.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GRNController : ControllerBase
    {
        private readonly IMediator _mediator;

        private readonly IGRNRepository _grnRepository; 
        public GRNController(IMediator mediator, 
            IGRNRepository gRNRepository)  
        {_mediator = mediator; 
            _grnRepository = gRNRepository;
        }

        [HttpPost("Save")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> Save([FromBody] CreateGRNCommand command)
        {
            // 🚀 SMART INJECTION: Get CompanyId & BranchId from Headers or Claims
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
            
            if (Guid.TryParse(companyIdHeader, out var companyId) || Guid.TryParse(companyIdClaim, out companyId))
            {
                command.Data.CompanyId = companyId;
            }

            var branchIdClaim = User.FindFirst("BranchId")?.Value;
            var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();

            string? finalBranchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
                ? branchIdHeader 
                : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

            if (!string.IsNullOrEmpty(finalBranchId))
            {
                command.Data.BranchId = finalBranchId;
            }

            string newGrnNumber = await _mediator.Send(command);

            if (!string.IsNullOrEmpty(newGrnNumber))
            {
                return Ok(new
                {
                    success = true,
                    message = "Stock Updated Successfully",
                    grnNumber = newGrnNumber 
                });
            }

            return BadRequest(new { success = false, message = "Failed to update stock" });
        }

        [HttpGet("GetPOData")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> GetPOData([FromQuery] string poIds, [FromQuery] Guid? grnHeaderId = null, [FromQuery] string? gatePassNo = null)
        {
            // Mediator query mein ab string poIds jayenge
            var data = await _mediator.Send(new GetPOForGRNQuery(poIds, grnHeaderId, gatePassNo));

            return data != null ? Ok(data) : NotFound("PO Not Found");
        }

        [HttpGet("grn-list")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> GetGRNList(
        [FromQuery] string? search = "",
        [FromQuery] string? sortField = "id", // Default value rakhein
        [FromQuery] string? sortOrder = "desc",
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool isQuick = false)
        {
            var result = await _mediator.Send(new GetGRNListQuery(search ?? "", sortField ?? "id", sortOrder ?? "desc", pageIndex, pageSize, isQuick));
            return Ok(result);
        }


        [HttpGet("print-data/{grnNumber}")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> GetPrintData(string grnNumber)
        {
            // String parameter receive kar rahe hain
            var result = await _grnRepository.GetGrnDetailsByNumberAsync(grnNumber);

            if (result == null)
                return NotFound(new { message = $"GRN {grnNumber} not found" });

            return Ok(result);
        }

        [HttpPost("bulk-create")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> CreateBulkGrn([FromBody] BulkGrnRequestDto request)
        {
            // 🚀 SMART INJECTION: Get CompanyId & BranchId from Headers or Claims
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
            
            if (Guid.TryParse(companyIdHeader, out var companyId) || Guid.TryParse(companyIdClaim, out companyId))
            {
                request.CompanyId = companyId;
            }

            var branchIdClaim = User.FindFirst("BranchId")?.Value;
            var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();

            string? finalBranchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
                ? branchIdHeader 
                : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

            if (!string.IsNullOrEmpty(finalBranchId))
            {
                request.BranchId = finalBranchId;
            }

            if (request.PurchaseOrderIds == null || !request.PurchaseOrderIds.Any())
                return BadRequest("No Purchase Orders selected.");

            var result = await _grnRepository.CreateBulkGrnFromPoAsync(request);

            if (result)
                return Ok(new { message = "Multiple GRNs created successfully!" });

            return StatusCode(500, "Error processing bulk GRNs.");
        }
    }

}

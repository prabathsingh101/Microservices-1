using Inventory.API.Common;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Common.Models;
using Inventory.Application.PriceLists.Commands.DeletePriceList;
using Inventory.Application.PriceLists.Commands.UpdatePriceList;
using Inventory.Application.PriceLists.DTOs;
using Inventory.Application.PriceLists.Queries.GetPriceListById;
using Inventory.Application.PriceLists.Queries.GetPriceLists;
using Inventory.Application.PriceLists.Queries.Paged;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [Route("api/pricelists")]
    [ApiController]
    public class PriceListsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IPriceListRepository _priceListRepository;


        public PriceListsController(IMediator mediator,IPriceListRepository price)
        {
            _mediator = mediator;
            _priceListRepository = price;
        }

        

        [HttpPost]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Create([FromBody] CreatePriceListCommand command)
        {
            // 🚀 SMART INJECTION: Get CompanyId & BranchId from Headers or Claims
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
            
            if (Guid.TryParse(companyIdHeader, out var companyId) || Guid.TryParse(companyIdClaim, out companyId))
            {
                command = command with { companyId = companyId };
            }

            var branchIdClaim = User.FindFirst("BranchId")?.Value;
            var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();

            string? finalBranchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
                ? branchIdHeader 
                : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

            if (!string.IsNullOrEmpty(finalBranchId))
            {
                command = command with { branchId = finalBranchId };
            }

            var resultId = await _mediator.Send(command);
            // Success object bhejien taaki frontend 'res.message' padh sake
            return Ok(new { success = true, message = "Price List saved successfully", id = resultId });
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Update(Guid id, UpdatePriceListCommand command)
        {
            if (id != command.id) return BadRequest("ID Mismatch");

            // 🚀 SMART INJECTION: Get CompanyId & BranchId from Headers or Claims
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
            
            if (Guid.TryParse(companyIdHeader, out var companyId) || Guid.TryParse(companyIdClaim, out companyId))
            {
                command = command with { companyId = companyId };
            }

            var branchIdClaim = User.FindFirst("BranchId")?.Value;
            var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();

            string? finalBranchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
                ? branchIdHeader 
                : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

            if (!string.IsNullOrEmpty(finalBranchId))
            {
                command = command with { branchId = finalBranchId };
            }

            var result = await _mediator.Send(command);
            return result ? Ok(new { message = "Updated successfully" }) : NotFound();
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _mediator.Send(
                new DeletePriceListCommand(id));

            return Ok(
                ApiResponse<Guid>.Ok(
                    result,
                    "Price list deleted successfully"
                )
            );
        }

        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetPriceListByIdQuery(id));
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetPriceListsQuery());
            return Ok(result);
        }

        [HttpGet("dropdown")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> dropdown([FromQuery] string? type)
        {
            // 🚀 SMART INJECTION: Get CompanyId from Claims
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            Guid? companyId = null;
            if (Guid.TryParse(companyIdClaim, out var cid))
            {
                companyId = cid;
            }

            var result = await _mediator.Send(new GetPriceListsLookUpQuery(companyId, type));
            return Ok(result);
        }

        [HttpGet("paged")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] GridRequest request)
        {
            var result = await _mediator.Send(
                new GetPriceListsPagedQuery(request)
            );

            return Ok(result);
        }

        [HttpPost("bulk-delete")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> BulkDelete([FromBody] List<Guid> ids)
        {
            await _mediator.Send(new BulkDeletePricelistsCommand(ids));

            return Ok(new
            {
                success = true,
                message = "Price lists deleted successfully"
            });
        }

        [HttpGet("price-list-items/{priceListId}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<ActionResult<List<PriceListItemDto>>> GetPriceListItems(Guid priceListId)
        {
           
            var items = await _priceListRepository.GetPriceListItemsAsync(priceListId);

           
            if (items == null)
            {
                return NotFound("No items found for this Price List.");
            }

            
            return Ok(items);
        }
    }
}

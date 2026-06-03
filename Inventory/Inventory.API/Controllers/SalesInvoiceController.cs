using Inventory.Application.SalesInvoices.Commands;
using Inventory.Application.SalesInvoices.DTOs;
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
    public class SalesInvoiceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SalesInvoiceController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("save")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> Save([FromBody] CreateSalesInvoiceDto dto)
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

            // Execute CQRS command
            var result = await _mediator.Send(new CreateSalesInvoiceCommand(dto));

            return Ok(result);
        }

        [HttpGet("get-paged")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> GetPaged(
             [FromQuery] string searchTerm = "",
             [FromQuery] int pageNumber = 1,
             [FromQuery] int pageSize = 10,
             [FromQuery] string sortBy = "Date",
             [FromQuery] string sortOrder = "desc",
             [FromQuery] DateTime? startDate = null,
             [FromQuery] DateTime? endDate = null,
             [FromQuery] string? branchId = null,
             [FromQuery] string? sourceFilter = null,
             [FromQuery] bool? isQuick = null)
        {
            var query = new Inventory.Application.SalesInvoices.Queries.GetUnifiedSalesInvoicesQuery
            {
                SearchTerm = searchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortBy = sortBy,
                SortOrder = sortOrder,
                StartDate = startDate,
                EndDate = endDate,
                BranchId = branchId,
                SourceFilter = sourceFilter,
                IsQuick = isQuick
            };

            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("get-items/{id}")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> GetItems(Guid id, [FromQuery] string source)
        {
            var query = new Inventory.Application.SalesInvoices.Queries.GetUnifiedSaleItemsQuery
            {
                Id = id,
                Source = source
            };

            var result = await _mediator.Send(query);
            return Ok(result);
        }
        [HttpDelete("{id}")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> Delete(Guid id, [FromQuery] string? reason)
        {
            var command = new Inventory.Application.SalesInvoices.Commands.DeleteSalesInvoice.DeleteSalesInvoiceCommand(id, reason);
            var result = await _mediator.Send(command);

            if (!result) return NotFound(new { message = "Invoice not found or could not be cancelled." });
            return Ok(new { success = true, message = "Invoice cancelled successfully." });
        }
    }
}

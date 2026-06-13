using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RequestForQuotationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RequestForQuotationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> Create([FromBody] CreateRfqDto dto)
        {
            // Extract CompanyId and BranchId from headers/claims
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
            Guid companyId = Guid.Empty;

            if (Guid.TryParse(companyIdHeader, out var parsedCompanyId) || Guid.TryParse(companyIdClaim, out parsedCompanyId))
            {
                companyId = parsedCompanyId;
            }

            var branchIdClaim = User.FindFirst("BranchId")?.Value;
            var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();
            string? branchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
                ? branchIdHeader 
                : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

            var updatedDto = dto with { CompanyId = companyId, BranchId = branchId, CreatedBy = userEmail };

            var result = await _mediator.Send(new CreateRfqCommand(updatedDto));
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRfqDto dto)
        {
            if (id != dto.Id) return BadRequest("Path ID does not match body ID.");

            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
            var updatedDto = dto with { ModifiedBy = userEmail };

            var result = await _mediator.Send(new UpdateRfqCommand(updatedDto));
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _mediator.Send(new DeleteRfqCommand(id));
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortField = null,
            [FromQuery] string? sortOrder = null,
            [FromQuery] string? filter = null,
            [FromQuery] bool isQuick = false)
        {
            var result = await _mediator.Send(new GetRfqsPagedQuery(pageIndex, pageSize, sortField, sortOrder, filter, isQuick));
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetRfqByIdQuery(id));
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("{id}/send")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> SendToSupplier(Guid id)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
            var result = await _mediator.Send(new SendRfqToSupplierCommand(id, userEmail));
            return Ok(result);
        }

        [HttpPost("{id}/confirm-rates")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> ConfirmRates(Guid id, [FromBody] List<ConfirmRfqItemRateDto> itemRates)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
            var result = await _mediator.Send(new ConfirmRfqRatesCommand(id, userEmail, itemRates));
            return Ok(result);
        }

        [HttpPost("{id}/convert-to-po")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> ConvertToPo(Guid id)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
            var result = await _mediator.Send(new ConvertRfqToPoCommand(id, userEmail));
            return Ok(new { success = true, purchaseOrderId = result });
        }

        [HttpGet("{id}/download-pdf")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> DownloadPdf(Guid id)
        {
            var pdfBytes = await _mediator.Send(new GetRfqPdfQuery(id));
            if (pdfBytes == null)
            {
                return NotFound(new { message = "RFQ document not found." });
            }

            string fileName = $"RFQ_{id}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}

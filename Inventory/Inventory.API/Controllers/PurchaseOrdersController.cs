using Inventory.Application.Common.Interfaces;
using Inventory.Application.Features.PurchaseOrders.Queries;
using Inventory.Application.PurchaseOrders.Commands.Delete;
using Inventory.Application.PurchaseOrders.Commands.Update;
using Inventory.Application.PurchaseOrders.DTOs;
using Inventory.Application.PurchaseOrders.Queries.GetNextPoNumber;
using Inventory.Application.PurchaseOrders.Queries.GetPurchaseOrder;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly IMediator _mediator;

        private readonly IPurchaseOrderRepository _purchaseOrderRepository;

        public PurchaseOrdersController(IMediator mediator, IPurchaseOrderRepository purchaseOrderRepository)
        {
            _mediator = mediator;
            _purchaseOrderRepository = purchaseOrderRepository;
        }

        [HttpGet("next-number")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> GetNextNumber()
        {
            // MediatR command bhej raha hai handler ko
            var result = await _mediator.Send(new GetNextPoNumberQuery());
            return Ok(new { poNumber = result });
        }

        [HttpPost("save-po")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderDto dto)
        {
            // 🚀 SMART INJECTION: Get CompanyId & BranchId from Headers or Claims (Header prioritised)
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
            
            if (Guid.TryParse(companyIdHeader, out var companyId) || Guid.TryParse(companyIdClaim, out companyId))
            {
                dto = dto with { CompanyId = companyId };
            }

            var branchIdClaim = User.FindFirst("BranchId")?.Value;
            var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();

            string? finalBranchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
                ? branchIdHeader 
                : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

            if (!string.IsNullOrEmpty(finalBranchId))
            {
                dto = dto with { BranchId = finalBranchId };
            }

            var result = await _mediator.Send(new CreatePurchaseOrderCommand(dto));

            if (result.Success)
                return Ok(new { success = true, id = result.Id, poNumber = result.PoNumber, message = "Purchase Order Draft saved successfully!" });

            return BadRequest(new { success = false, message = "Failed to save PO." });
        }

        [HttpGet]
        //[Authorize(Roles = "Manager, Admin")]
        public async Task<ActionResult> GetOrders([FromQuery] GetPurchaseOrdersQuery query)
        {
            // Ensure 'query' is not null
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPost("query")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> GetOrders([FromBody] GetPurchaseOrdersRequest request)
        {
            var query = new GetDateRangePurchaseOrdersQuery(request);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPost("get-paged-orders")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> GetPagedOrders([FromBody] GetPurchaseOrdersRequest request)
        {
            // Frontend se aane wale request DTO ko query mein wrap kar rahe hain
            var query = new GetDateRangePurchaseOrdersQuery(request);

            // Mediator isse sahi Handler tak pahuchayega
            var response = await _mediator.Send(query);

            return Ok(response);
        }

        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            // MediatR ke through Query ko Handler tak bhejna
            var query = new GetPurchaseOrderByIdQuery(id);
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(new
                {
                    status = "error",
                    message = $"Purchase Order with ID {id} not found."
                });
            }

            return Ok(result);
        }

        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        [HttpPut("{id}")] //
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePurchaseOrderDto dto)
        {
            // 1. Validation: URL ID aur Body ID match honi chahiye
            if (id != dto.Id)
            {
                return BadRequest(new { message = "ID mismatch between URL and body." });
            }

            // 🚀 SMART INJECTION: Get CompanyId & BranchId from Headers or Claims
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

            // 2. Command Create karna
            var command = new UpdatePurchaseOrderCommand(dto);

            // 3. Mediator ke through handler ko call karna
            var result = await _mediator.Send(command);

            if (!result)
            {
                return NotFound(new { message = $"Purchase Order with ID {id} not found or update failed." });
            }

            // 4. Success Response
            return Ok(new
            {
                status = "success",
                message = "Purchase Order updated successfully"
            });
        }



        /// <summary>
        /// URL: DELETE /api/PurchaseOrders/{id}
        /// ye single record delete karega
        /// Frontend call: this.http.delete(`${this.apiUrl}/PurchaseOrders/${poId}`)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                // 1. Mediator ke through Command bhej rahe hain Handler ko
                var result = await _mediator.Send(new DeletePurchaseOrderCommand(id));

                if (!result)
                {
                    return NotFound(new { success = false, message = "This PO is not found in database." });
                }

                // 2. Agar success hua toh 200 OK
                return Ok(new { success = true, message = "Purchase Order deleted successfully." });
            }
            catch (InvalidOperationException ex)
            {
                // 3. Domain Rule fail hua (e.g., Status 'Received' tha)
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                // 4. Koi aur technical error
                return StatusCode(500, new { success = false, message = "Internal server error: " + ex.Message });
            }
        }


        // --- 2. BULK PARENT DELETE ---
        // URL: POST /api/PurchaseOrders/bulk-delete
        // Frontend call: this.http.post(`${this.apiUrl}/PurchaseOrders/bulk-delete`, { ids })
        [HttpPost("bulk-delete-orders")] // Name easily identifiable
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> BulkDeleteOrders([FromBody] BulkDeletePurchaseOrderCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                return Ok(new { success = true, message = "Selected orders is deleted!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // --- 3. BULK CHILD ITEMS DELETE ---
        [HttpPost("bulk-delete-items")] // Easily identifiable name
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> BulkDeleteItems([FromBody] BulkDeletePOItemsCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                if (!result) return NotFound(new { success = false, message = "Did not found PO items." });

                return Ok(new { success = true, message = "Selected items successfully removed!" });
            }
            catch (InvalidOperationException ex)
            {
                // Agar status "Received" nikla toh ye error throw karega
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Update status
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>

        [HttpPut("UpdateStatus")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusDTO dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Status))
                return BadRequest("Data sahi nahi hai");

            // 🚀 SMART INJECTION: Get CompanyId & BranchId from Headers or Claims
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

            var command = new UpdatePOStatusCommand(dto.Id, dto.Status);
            var result = await _mediator.Send(command);

            if (result)
                return Ok(new { message = "Status Updated to " + dto.Status });

            return NotFound("PO nahi mila");
        }

        [HttpGet("pending-pos")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> GetPendingPOs()
        {
            var result = await _mediator.Send(new GetPendingPOQuery());
            return Ok(result);
        }

        [HttpGet("po-items/{poId}")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> GetPOItemsForGRN(Guid poId)
        {
            var result = await _mediator.Send(new GetPOItemsForGRNQuery(poId));
            return Ok(result);
        }

        /// <summary>
        /// Dashboard se lastPurchaseOrderId (int) lekar Header details fetch karta hai
        /// </summary>
        /// <param name="lastPurchaseOrderId">Integer format ID</param>
        [HttpGet("header-details/{lastPurchaseOrderId:guid}")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<ActionResult<POHeaderDetailsDto>> GetHeaderDetails(Guid lastPurchaseOrderId)
        {
            // 1. Query create karein [cite: 2026-01-22]
            var query = new GetPOHeaderDetailsQuery(lastPurchaseOrderId);

            // 2. MediatR se Handler trigger karein [cite: 2026-01-22]
            var result = await _mediator.Send(query);

            // 3. Validation
            if (result == null)
            {
                return NotFound($"Previous Purchase Order with ID {lastPurchaseOrderId} not found.");
            }

            // 4. POHeaderDetailsDto return karein
            return Ok(result);
        }

        /// <summary>
        /// Product select hone par ya Price List change hone par rate fetch karne ke liye
        /// </summary>
        [HttpGet("get-product-rate")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<ActionResult<ProductPriceDto>> GetProductRate(
            
            [FromQuery] Guid productId, [FromQuery] Guid priceListId, [FromQuery] string? type)
        {
            // Validation: Product ID honi chahiye
            if (productId == Guid.Empty)
            {
                return BadRequest(new { message = "Invalid Product selection." });
            }

            // Repository call
            var result = await _purchaseOrderRepository.GetPriceListRateAsync(productId, priceListId, type);

            if (result == null)
            {
                // Agar item price list mein nahi hai
                return NotFound(new
                {
                    message = "This product is not registered in the selected Price List."
                });
            }

            // Success: Rate, Unit aur DefaultGst return karega
            return Ok(result);
        }

        [HttpPost("bulk-sent-for-approval")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> BulkSentForApproval([FromBody] List<Guid> ids)
        {
            var result = await _purchaseOrderRepository.BulkSentForApprovalAsync(ids);
            if (result) return Ok(new { message = "Selected POs sent for approval successfully." });
            return BadRequest(new { message = "No valid Draft POs found in selection." });
        }
        

        [HttpPost("bulk-approve")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> BulkApprove([FromBody] List<Guid> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest(new { message = "No POs selected for approval." });

            var userEmail = User.Identity?.Name ?? "Admin";

            var result = await _purchaseOrderRepository.BulkApprovePOsAsync(ids, userEmail);

            if (result)
                return Ok(new { message = $"{ids.Count} Purchase Orders approved successfully." });

            return BadRequest(new { message = "Approval failed. Please ensure selected POs are in 'Submitted' status." });
        }

       

        [HttpPost("bulk-reject")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> BulkReject([FromBody] List<Guid> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest(new { message = "No POs selected for rejection." });

            // Current user ka identity nikaalein
            var userEmail = User.Identity?.Name ?? "Manager";

            var result = await _purchaseOrderRepository.BulkRejectPOsAsync(ids, userEmail);

            if (result)
                return Ok(new { message = $"{ids.Count} Purchase Orders rejected successfully." });

            return BadRequest(new { message = "Rejection failed. Only 'Submitted' POs can be rejected." });
        }

        /// <summary>
        /// Print po
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpGet("{id}/print-details")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> GetPrintDetails(Guid id)
        {
            var result = await _purchaseOrderRepository.GetPODetailsForPrintAsync(id);

            if (result == null)
                return NotFound(new { message = "Purchase Order not found." });

            return Ok(result);
        }

        /// <summary>
        /// download pdf
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpGet("{id}/download-pdf")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> DownloadPdf(Guid id)
        {
            
            var response = await _purchaseOrderRepository.GeneratePOReportPdfAsync(id);
          
            if (response == null || response.PdfBytes == null)
            {
                return NotFound(new { message = "Purchase Order document not found." });
            }
          
            string safeTitle = response.HeaderTitle?.Replace(" ", "_") ?? "PO_Document";
            string fileName = $"{safeTitle}_{id}_{DateTime.Now:yyyyMMdd}.pdf";
           
            return File(response.PdfBytes, "application/pdf", fileName);
        }

        [HttpGet("replacement-qty/{poId}")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> GetReplacementQty(Guid poId)
        {
            var qty = await _purchaseOrderRepository.GetTotalReturnedQtyAsync(poId);
            return Ok(new { replacementQty = qty });
        }
        [HttpPut("{id}/toggle-dispatch")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse")]
        public async Task<IActionResult> ToggleDispatch(Guid id)
        {
            var result = await _purchaseOrderRepository.ToggleDispatchStatusAsync(id);
            if (result)
                return Ok(new { success = true, message = "Dispatch status updated successfully." });

            return NotFound(new { success = false, message = "Purchase Order not found." });
        }
    }
}

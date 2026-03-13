using Inventory.Application.PurchaseReturn;
using Inventory.Application.PurchaseReturn.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Inventory.Application.Common.Interfaces;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseReturnController : ControllerBase
    {
        private readonly IPurchaseReturnRepository _repository;

        public PurchaseReturnController(IPurchaseReturnRepository repository)
        {
            _repository = repository;
        }


        // GET: api/PurchaseReturn/rejected-items/{supplierId}
        // Recommended Route
        [HttpGet("rejected-items/{supplierId}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetRejectedItems(int supplierId)
        {
            try
            {
                var items = await _repository.GetRejectedItemsBySupplierAsync(supplierId);
                
                // FIX: API should return Empty List (Ok) instead of Error (NotFound)
                // This prevents ForkJoin from failing on Frontend [cite: PR List Fix]
                if (items == null) items = new List<RejectedItemDto>();
                
                return Ok(items);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error fetching items", error = ex.Message });
            }
        }


        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        [HttpGet("suppliers-purchase-return")]
        public async Task<IActionResult> GetSuppliersWithRejections()
        {
            try
            {
                var suppliers = await _repository.GetSuppliersForPurchaseReturnAsync();

                if (suppliers == null || suppliers.Count == 0)
                    return Ok(new List<SupplierSelectDto>()); // Khali list bhejein agar koi rejection nahi hai

                return Ok(suppliers);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Suppliers load karne mein dikkat aayi", error = ex.Message });
            }
        }

        [HttpGet("get-received-stock/{supplierId}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetReceivedStock(int supplierId)
        {
            try
            {
                var result = await _repository.GetReceivedStockBySupplierAsync(supplierId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error fetching received stock", error = ex.Message });
            }
        }


        // POST: api/PurchaseReturn/create
        [HttpPost("create")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> CreateReturn([FromBody] PurchaseReturnDto returnDto)
        {
            if (returnDto == null || returnDto.Items == null || !returnDto.Items.Any())
            {
                return BadRequest("Invalid return data. No items selected.");
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // DTO ko Entity mein map karein [cite: 2026-02-04]
                var returnEntity = new Inventory.Domain.Entities.PurchaseReturn
                {
                    SupplierId = returnDto.SupplierId,
                    ReturnDate = returnDto.ReturnDate,
                    Remarks = returnDto.Remarks,
                    GrandTotal = 0,
                    Status = "Confirmed",
                    Items = new List<Inventory.Domain.Entities.PurchaseReturnItem>()
                };

                foreach (var item in returnDto.Items)
                {
                    // Calculation [cite: 2026-02-04]
                    var itemTotal = item.ReturnQty * item.Rate;
                    returnEntity.GrandTotal += itemTotal;

                    returnEntity.Items.Add(new Inventory.Domain.Entities.PurchaseReturnItem
                    {
                        ProductId = item.ProductId,
                        GrnRef = item.GrnRef,
                        ReturnQty = item.ReturnQty,
                        Rate = item.Rate,
                        TotalAmount = itemTotal,
                        MfgDate = item.MfgDate,
                        ExpDate = item.ExpDate
                    });
                }

                // Repository call [cite: 2026-02-04]
                var result = await _repository.CreatePurchaseReturnAsync(returnEntity);

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Purchase Return successfully created!",
                        returnNumber = returnEntity.ReturnNumber
                    });
                }

                return StatusCode(500, "Database save operation failed.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal Server Error", error = ex.Message });
            }
        }

        [HttpGet("list")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<ActionResult<PurchaseReturnPagedResponse>> GetList(
             [FromQuery] string? filter,        // Frontend se 'filter' key aa rahi hai [cite: 2026-02-04]
             [FromQuery] int pageIndex = 0,
             [FromQuery] int pageSize = 10,
             [FromQuery] DateTime? fromDate = null,
             [FromQuery] DateTime? toDate = null,
             [FromQuery] string? status = null,
             [FromQuery] string? sortField = "ReturnDate",
             [FromQuery] string? sortOrder = "desc")
        {
            // Repository ko saare parameters pass karein [cite: 2026-02-04]
            var result = await _repository.GetPurchaseReturnsAsync(
                filter,
                pageIndex,
                pageSize,
                fromDate,
                toDate,
                status,
                sortField,
                sortOrder);

            return Ok(result);
        }

        [HttpGet("details/{id}")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetById(Guid id)        
        {
           
            var result = await _repository.GetPurchaseReturnByIdAsync(id);
           
            if (result == null)
            {
                return NotFound(new { message = "Purchase Return record not found." });
            }
           
            return Ok(result);
        }

        [HttpGet("export-excel")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> ExportExcel([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var fileContent = await _repository.ExportPurchaseReturnsToExcelAsync(fromDate, toDate);
            string fileName = $"DebitNotes_{DateTime.Now:yyyyMMdd}.xlsx";

            // File return karein proper MIME type ke saath [cite: 2026-02-04]
            return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("pending-prs")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetPendingPRs()
        {
            var result = await _repository.GetPendingPurchaseReturnsAsync();
            return Ok(result);
        }

        [HttpPost("bulk-outward")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> BulkOutward([FromBody] List<Guid> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("No IDs provided");

            var result = await _repository.BulkOutwardAsync(ids);
            return result ? Ok(new { message = $"{ids.Count} Returns Outwarded successfully" }) : BadRequest("Could not process outward");
        }

        [HttpGet("summary")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetSummary()
        {
            var result = await _repository.GetPurchaseReturnSummaryAsync();
            return Ok(result);
        }
    }
}

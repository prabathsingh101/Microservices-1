using Inventory.Application.Common.Interfaces;
using Inventory.Application.GRN.Command;
using Inventory.Application.GRN.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Inventory.API.Hubs;
using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Entities;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GRNController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IGRNRepository _grnRepository; 
        private readonly IHubContext<DeliveryHub> _hubContext;
        private readonly IInventoryDbContext _context;

        public GRNController(IMediator mediator, 
            IGRNRepository gRNRepository,
            IHubContext<DeliveryHub> hubContext,
            IInventoryDbContext context)  
        {
            _mediator = mediator; 
            _grnRepository = gRNRepository;
            _hubContext = hubContext;
            _context = context;
        }

        [HttpPost("Save")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
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
                var productIds = command.Data.Items != null ? command.Data.Items.Select(i => i.ProductId).ToList() : new List<Guid>();
                await BroadcastStockUpdatesAsync(productIds, command.Data.BranchId, companyId);

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
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> GetPOData([FromQuery] string poIds, [FromQuery] Guid? grnHeaderId = null, [FromQuery] string? gatePassNo = null)
        {
            // Mediator query mein ab string poIds jayenge
            var data = await _mediator.Send(new GetPOForGRNQuery(poIds, grnHeaderId, gatePassNo));

            return data != null ? Ok(data) : NotFound("PO Not Found");
        }

        [HttpGet("grn-list")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
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
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> GetPrintData(string grnNumber)
        {
            // String parameter receive kar rahe hain
            var result = await _grnRepository.GetGrnDetailsByNumberAsync(grnNumber);

            if (result == null)
                return NotFound(new { message = $"GRN {grnNumber} not found" });

            return Ok(result);
        }

        [HttpPost("bulk-create")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
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
            {
                var productIds = request.Items != null ? request.Items.Select(i => i.ProductId).ToList() : new List<Guid>();
                await BroadcastStockUpdatesAsync(productIds, request.BranchId, companyId);

                return Ok(new { message = "Multiple GRNs created successfully!" });
            }

            return StatusCode(500, "Error processing bulk GRNs.");
        }

        [HttpGet("rejection-history/{grnNumber}")]
        [Authorize(Roles = "Super Admin, Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> GetRejectionHistory([FromRoute] string grnNumber)
        {
            var result = await _grnRepository.GetGrnRejectionHistoryAsync(grnNumber);
            return Ok(result);
        }

        public class CancelDto
        {
            public string? Reason { get; set; }
        }


        [HttpPut("cancel/{id}")]
        [Authorize(Roles = "Super Admin, Admin, Manager")]
        public async Task<IActionResult> CancelGRN([FromRoute] Guid id, [FromBody] CancelDto dto)
        {
            var command = new CancelGRNCommand
            {
                GrnId = id,
                CancelledBy = User.FindFirst("UserId")?.Value ?? "System",
                Reason = dto?.Reason
            };

            var success = await _mediator.Send(command);

            if (success)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveInventoryUpdate");
                return Ok(new { success = true, message = "Invoice Cancelled & Stock Reversed Successfully" });
            }

            return BadRequest(new { success = false, message = "Failed to cancel invoice" });
        }

        private async Task BroadcastStockUpdatesAsync(IEnumerable<Guid> productIds, string? branchId, Guid companyId)
        {
            try
            {
                var pIds = productIds.Distinct().ToList();
                if (!pIds.Any()) return;

                var stockQuery = _context.WarehouseStocks
                    .IgnoreQueryFilters()
                    .Where(ws => ws.CompanyId == companyId && pIds.Contains(ws.ProductId));

                if (!string.IsNullOrEmpty(branchId))
                {
                    stockQuery = stockQuery.Where(ws => ws.BranchId == branchId);
                }

                var stockList = await stockQuery
                    .GroupBy(ws => ws.ProductId)
                    .Select(g => new { ProductId = g.Key, CurrentStock = g.Sum(x => x.Quantity) })
                    .ToListAsync();

                if (!string.IsNullOrEmpty(branchId))
                {
                    await _hubContext.Clients.Group(branchId).SendAsync("ReceiveInventoryUpdate", stockList);
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveInventoryUpdate", stockList);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error broadcasting stock updates: {ex.Message}");
                if (!string.IsNullOrEmpty(branchId))
                {
                    await _hubContext.Clients.Group(branchId).SendAsync("ReceiveInventoryUpdate");
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveInventoryUpdate");
                }
            }
        }
    }
}

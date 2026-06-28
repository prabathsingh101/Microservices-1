using Inventory.Application.Stock.Commands;
using Inventory.Application.Stock.Commands.RejectStock;
using Inventory.Application.Stock.Commands.MoveToExpiredRack;
using Inventory.Application.GRN.Command;
using Inventory.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Inventory.API.Hubs;
using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Entities;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IStockRepository _stockRepo;
        private readonly IHubContext<DeliveryHub> _hubContext;
        private readonly IInventoryDbContext _context;

        public StockController(IMediator mediator, IStockRepository stockRepo, IHubContext<DeliveryHub> hubContext, IInventoryDbContext context)
        {
            _mediator = mediator;
            _stockRepo = stockRepo;
            _hubContext = hubContext;
            _context = context;
        }

        [HttpGet("current-stock")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> GetStock(
           [FromQuery] string? search,
           [FromQuery] string? sortField,
           [FromQuery] string? sortOrder,
           [FromQuery] DateTime? startDate,
           [FromQuery] DateTime? endDate,
           [FromQuery] Guid? warehouseId,
           [FromQuery] Guid? rackId,
           [FromQuery] bool showPurged = false,
           [FromQuery] int pageIndex = 0,
           [FromQuery] int pageSize = 10,
           [FromQuery] string? branchId = null)
        {
            var command = new GetCurrentStockCommand(search, sortField, sortOrder, pageIndex, pageSize, startDate, endDate, warehouseId, rackId, showPurged, branchId);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("ExportExcel")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> ExportExcel([FromBody] List<Guid> productIds)
        {
            if (productIds == null || !productIds.Any())
                return BadRequest("No products selected.");

            try
            {
                var fileContent = await _stockRepo.GenerateStockExcel(productIds);
                var indianTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                string fileName = $"StockReport_{indianTime:yyyyMMdd_HHmm}.xlsx";

                return File(
                    fileContent,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("adjust")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> AdjustStock([FromBody] RejectStockCommand command)
        {
            var result = await _mediator.Send(command);
            if (result)
            {
                var companyIdClaim = User.FindFirst("CompanyId")?.Value;
                var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
                Guid companyId = Guid.Empty;
                if (Guid.TryParse(companyIdHeader, out var parsedCompanyId) || Guid.TryParse(companyIdClaim, out parsedCompanyId))
                {
                    companyId = parsedCompanyId;
                }
                await BroadcastStockUpdatesAsync(new[] { command.ProductId }, command.BranchId, companyId);
            }
            return Ok(new { success = result, message = result ? "Stock adjusted successfully" : "Failed to adjust stock. Item not found or insufficient quantity." });
        }

        [HttpPost("move-to-expired")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> MoveToExpired([FromBody] MoveToExpiredRackCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                if (result)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveInventoryUpdate");
                }
                return Ok(new { success = result, message = "Batch moved to Expired Rack successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


        [HttpGet("batch-history")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> GetBatchHistory(
            [FromQuery] Guid productId,
            [FromQuery] Guid warehouseId,
            [FromQuery] Guid rackId,
            [FromQuery] DateTime? mfgDate,
            [FromQuery] DateTime? expDate,
            [FromQuery] string? branchId = null)
        {
            var result = await _stockRepo.GetBatchTransactionsAsync(productId, warehouseId, rackId, mfgDate, expDate, branchId);
            return Ok(result);
        }

        [HttpPost("sync")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> Sync()
        {
            var result = await _mediator.Send(new SyncStockCommand());
            if (result)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveInventoryUpdate");
            }
            return Ok(new { success = result, message = "Stock synchronized successfully." });
        }

        [HttpGet("warehouse-stock")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> GetWarehouseStock(
           [FromQuery] string? search,
           [FromQuery] string? sortField,
           [FromQuery] string? sortOrder,
           [FromQuery] int pageIndex = 0,
           [FromQuery] int pageSize = 10,
           [FromQuery] Guid? productId = null,
           [FromQuery] Guid? warehouseId = null,
           [FromQuery] string? branchId = null)
        {
            var result = await _stockRepo.GetWarehouseStockAsync(search, sortField, sortOrder, pageIndex, pageSize, productId, warehouseId, branchId);
            return Ok(result);
        }

        [HttpGet("disposed-stock")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> GetDisposedStock(
           [FromQuery] string? search,
           [FromQuery] string? sortField,
           [FromQuery] string? sortOrder,
           [FromQuery] DateTime? startDate,
           [FromQuery] DateTime? endDate,
           [FromQuery] Guid? warehouseId,
           [FromQuery] Guid? rackId,
           [FromQuery] int pageIndex = 0,
           [FromQuery] int pageSize = 10,
           [FromQuery] string? branchId = null)
        {
            var result = await _stockRepo.GetDisposedStockAsync(search, sortField, sortOrder, pageIndex, pageSize, startDate, endDate, warehouseId, rackId, branchId);
            return Ok(result);
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

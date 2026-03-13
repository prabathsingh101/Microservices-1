using Inventory.Application.Stock.Commands.RejectStock;
using Inventory.Application.Stock.Commands.MoveToExpiredRack;
using Inventory.Application.GRN.Command;
using Inventory.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IStockRepository _stockRepo;

        public StockController(IMediator mediator, IStockRepository stockRepo)
        {
            _mediator = mediator;
            _stockRepo = stockRepo;
        }

        [HttpGet("current-stock")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetStock(
           [FromQuery] string? search,
           [FromQuery] string? sortField,
           [FromQuery] string? sortOrder,
           [FromQuery] DateTime? startDate,
           [FromQuery] DateTime? endDate,
           [FromQuery] Guid? warehouseId,
           [FromQuery] Guid? rackId,
           [FromQuery] int pageIndex = 0,
           [FromQuery] int pageSize = 10)
        {
            var command = new GetCurrentStockCommand(search, sortField, sortOrder, pageIndex, pageSize, startDate, endDate, warehouseId, rackId);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("ExportExcel")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
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
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> AdjustStock([FromBody] RejectStockCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(new { success = result, message = result ? "Stock adjusted successfully" : "Failed to adjust stock. Item not found or insufficient quantity." });
        }

        [HttpPost("move-to-expired")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> MoveToExpired([FromBody] MoveToExpiredRackCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                return Ok(new { success = result, message = "Batch moved to Expired Rack successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("disposed-stock")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetDisposedStock(
           [FromQuery] string? search,
           [FromQuery] string? sortField,
           [FromQuery] string? sortOrder,
           [FromQuery] DateTime? startDate,
           [FromQuery] DateTime? endDate,
           [FromQuery] Guid? warehouseId,
           [FromQuery] Guid? rackId,
           [FromQuery] int pageIndex = 0,
           [FromQuery] int pageSize = 10)
        {
            var result = await _stockRepo.GetDisposedStockAsync(search, sortField, sortOrder, pageIndex, pageSize, startDate, endDate, warehouseId, rackId);
            return Ok(result);
        }
    }
}

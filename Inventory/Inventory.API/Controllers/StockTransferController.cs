using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockTransferController : ControllerBase
    {
        private readonly IStockTransferRepository _repository;
        private readonly ICurrentUserService _currentUserService;

        public StockTransferController(IStockTransferRepository repository, ICurrentUserService currentUserService)
        {
            _repository = repository;
            _currentUserService = currentUserService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateTransfer([FromBody] StockTransferRequest request)
        {
            try
            {
                var companyId = _currentUserService.CompanyId ?? Guid.Empty;
                
                var header = new StockTransferHeader(
                    request.TransferNumber ?? "",
                    request.TransferDate ?? DateTime.UtcNow,
                    request.FromWarehouseId,
                    request.ToWarehouseId,
                    request.FromBranchId,
                    request.ToBranchId,
                    companyId,
                    request.Remarks
                );

                var details = request.Items.Select(i => new StockTransferDetail(
                    i.ProductId,
                    i.Quantity,
                    i.BatchNumber,
                    companyId,
                    request.FromBranchId // Log under source branch initially
                )).ToList();

                var result = await _repository.CreateTransferAsync(header, details);
                return Ok(new { transferNumber = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetList()
        {
            var list = await _repository.GetTransferListAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var data = await _repository.GetTransferByIdAsync(id);
            if (data == null) return NotFound();
            return Ok(data);
        }

        [HttpPost("receive")]
        public async Task<IActionResult> ReceiveTransfer([FromBody] ReceiveTransferRequest request)
        {
            try
            {
                var success = await _repository.ReceiveTransferAsync(request.TransferId, request.Remarks);
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class ReceiveTransferRequest
    {
        public Guid TransferId { get; set; }
        public string? Remarks { get; set; }
    }

    public class StockTransferRequest
    {
        public string? TransferNumber { get; set; }
        public DateTime? TransferDate { get; set; }
        public Guid FromWarehouseId { get; set; }
        public Guid ToWarehouseId { get; set; }
        public string? FromBranchId { get; set; }
        public string? ToBranchId { get; set; }
        public string? Remarks { get; set; }
        public List<StockTransferItemRequest> Items { get; set; } = new();
    }

    public class StockTransferItemRequest
    {
        public Guid ProductId { get; set; }
        public decimal Quantity { get; set; }
        public string? BatchNumber { get; set; }
    }
}

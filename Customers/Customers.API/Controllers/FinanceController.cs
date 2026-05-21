using Customers.Application.DTOs;
using Customers.Application.Features.Finance.Commands;
using Customers.Application.Features.Finance.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Customers.API.Controllers
{
    [Route("api/finance")]
    [ApiController]
    [Authorize]
    public class FinanceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FinanceController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // 1. Customer Ledger
        [HttpPost("ledger")]
        //[Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> GetLedger([FromBody] CustomerLedgerRequestDto request)
        {
            var result = await _mediator.Send(new GetCustomerLedgerQuery(request));
            return Ok(result);
        }

        // 2. Receipt Entry
        [HttpPost("receipt")]
        //[Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> RecordReceipt([FromBody] CustomerReceiptDto receiptDto)
        {
            var command = new RecordCustomerReceiptCommand(receiptDto);
            var id = await _mediator.Send(command);
            
            return Ok(new { Id = id }); // Returning object to be consistent
        }

        // 2a. Bulk Receipt Entry
        [HttpPost("bulk-receipts")]
        public async Task<IActionResult> RecordBulkReceipts([FromBody] BulkReceiptDto bulkReceiptDto)
        {
            var command = new BulkRecordCustomerReceiptCommand(bulkReceiptDto.Receipts);
            var result = await _mediator.Send(command);
            return Ok(new { Success = result });
        }

        // 2b. Sale Entry (called from Inventory when Sale is confirmed)
        [HttpPost("sale")]
        //[Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> RecordSale([FromBody] CustomerSaleDto saleDto)
        {
            var command = new RecordCustomerSaleCommand(saleDto);
            var id = await _mediator.Send(command);
            return Ok(new { Id = id });
        }

        // 3. Outstanding Tracker
        [HttpPost("outstanding")]
        //[Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Salesman")]
        public async Task<IActionResult> GetOutstanding([FromBody] OutstandingRequestDto request)
        {
            var result = await _mediator.Send(new GetOutstandingQuery(request));
            return Ok(result);
        }

        [HttpGet("outstanding-total")]
        public async Task<IActionResult> GetOutstandingTotal([FromQuery] string? branchId = null, [FromQuery] string? companyId = null)
        {
            var total = await _mediator.Send(new GetTotalOutstandingQuery(branchId, companyId));
            return Ok(new { TotalOutstanding = total });
        }

        [HttpGet("pending-dues")]
        public async Task<IActionResult> GetPendingDues([FromQuery] string? branchId = null)
        {
            var result = await _mediator.Send(new GetPendingDuesQuery(branchId));
            return Ok(result);
        }

        // 4. Total Receipts (For P&L)
        [HttpPost("total-receipts")]
        public async Task<IActionResult> GetTotalReceipts([FromBody] DateRangeDto dateRange)
        {
            if (string.IsNullOrEmpty(dateRange.BranchId)) {
                dateRange.BranchId = Request.Headers["X-Branch-Id"].ToString();
            }
            var total = await _mediator.Send(new GetTotalReceiptsQuery(dateRange));
            return Ok(new { TotalReceipts = total });
        }

        [HttpPost("total-adjustments")]
        public async Task<IActionResult> GetTotalAdjustments([FromBody] DateRangeDto dateRange)
        {
            if (string.IsNullOrEmpty(dateRange.BranchId)) {
                dateRange.BranchId = Request.Headers["X-Branch-Id"].ToString();
            }
            var result = await _mediator.Send(new GetTotalAdjustmentsQuery(dateRange));
            return Ok(result);
        }

        [HttpGet("monthly-receipts")]
        public async Task<IActionResult> GetMonthlyReceipts([FromQuery] int months = 6, [FromQuery] string? branchId = null)
        {
            var result = await _mediator.Send(new GetMonthlyReceiptsTrendQuery(months, branchId));
            return Ok(result);
        }

        [HttpPost("receipts-report")]
        public async Task<IActionResult> GetReceiptsReport([FromBody] ReceiptReportRequestDto request)
        {
            var result = await _mediator.Send(new GetReceiptsReportQuery(request));
            return Ok(result);
        }
    }
}

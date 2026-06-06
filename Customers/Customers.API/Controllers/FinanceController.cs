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

        // 2.1 Refund Entry
        [HttpPost("refund")]
        public async Task<IActionResult> RecordRefund([FromBody] CustomerRefundDto refundDto)
        {
            var command = new RecordCustomerRefundCommand(refundDto);
            var id = await _mediator.Send(command);
            
            return Ok(new { Id = id });
        }

        // 2.2 Delete Receipt/Refund Entry
        [HttpDelete("receipt/{id}")]
        [Authorize(Roles = "Super Admin, Admin, Manager")]
        public async Task<IActionResult> DeleteReceipt(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest("Invalid Receipt Id");
            }
            var command = new DeleteCustomerReceiptCommand(id);
            var success = await _mediator.Send(command);
            if (!success)
            {
                return NotFound("Receipt not found");
            }
            return Ok(new { Success = true });
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
        public async Task<IActionResult> GetPendingDues([FromQuery] string? branchId = null, [FromQuery] string? companyId = null)
        {
            var result = await _mediator.Send(new GetPendingDuesQuery(branchId, companyId));
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

        // ---- DEBTORS AGEING ----
        [HttpGet("debtors-ageing")]
        public async Task<IActionResult> GetDebtorsAgeing([FromQuery] string? branchId = null)
        {
            var result = await _mediator.Send(new GetDebtorsAgeingQuery(branchId));
            return Ok(result);
        }

        // ---- PAYMENT REMINDERS ----
        [HttpGet("payment-reminder-logs")]
        public async Task<IActionResult> GetPaymentReminderLogs([FromQuery] Guid? customerId = null, [FromQuery] string? branchId = null)
        {
            var result = await _mediator.Send(new GetPaymentReminderLogsQuery(customerId, branchId));
            return Ok(result);
        }

        [HttpPost("payment-reminders")]
        public async Task<IActionResult> RecordPaymentReminder([FromBody] PaymentReminderLogDto reminderDto)
        {
            var id = await _mediator.Send(new RecordPaymentReminderCommand(reminderDto));
            return Ok(new { Id = id, Message = "Reminder logged successfully" });
        }

        // ---- CONTRA ENTRIES ----
        [HttpGet("contra-entries")]
        public async Task<IActionResult> GetContraEntries([FromQuery] string? branchId = null)
        {
            var result = await _mediator.Send(new GetContraEntriesQuery(branchId));
            return Ok(result);
        }

        [HttpPost("contra-entries")]
        public async Task<IActionResult> RecordContraEntry([FromBody] ContraEntryDto contraDto)
        {
            var id = await _mediator.Send(new RecordContraEntryCommand(contraDto));
            return Ok(new { Id = id, Message = "Contra entry recorded successfully" });
        }

        // ---- BANK RECONCILIATION ----
        [HttpGet("reconciliation/statements")]
        public async Task<IActionResult> GetBankStatements([FromQuery] string? branchId = null)
        {
            var result = await _mediator.Send(new GetBankStatementsQuery(branchId));
            return Ok(result);
        }

        [HttpGet("reconciliation/statements/{statementId}/lines")]
        public async Task<IActionResult> GetBankStatementLines(Guid statementId)
        {
            var result = await _mediator.Send(new GetBankStatementLinesQuery(statementId));
            return Ok(result);
        }

        [HttpPost("reconciliation/upload")]
        public async Task<IActionResult> UploadBankStatement([FromBody] UploadBankStatementCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(new { Id = id, Message = "Bank statement uploaded successfully" });
        }

        [HttpPost("reconciliation/reconcile")]
        public async Task<IActionResult> ReconcileTransaction([FromBody] ReconcileTransactionRequestDto requestDto)
        {
            var success = await _mediator.Send(new ReconcileTransactionCommand(requestDto));
            if (!success) return BadRequest(new { Message = "Statement line not found or could not be reconciled." });
            return Ok(new { Message = "Transaction reconciled successfully" });
        }

        [HttpGet("reconciliation/unmatched")]
        public async Task<IActionResult> GetUnmatchedTransactions([FromQuery] string transactionType = "CustomerReceipt", [FromQuery] string? branchId = null)
        {
            var result = await _mediator.Send(new GetUnmatchedSystemTransactionsQuery(transactionType, branchId));
            return Ok(result);
        }
    }
}

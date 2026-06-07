using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Suppliers.Application.DTOs;
using Suppliers.Application.Features.Suppliers.Commands;
using Suppliers.Application.Features.Suppliers.Queries;
using Suppliers.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Suppliers.Application.Common.Interfaces;

namespace Suppliers.API.Controllers
{
    [Route("api/finance")]
    [ApiController]
    [Authorize]
    public class FinanceController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;

        public FinanceController(IMediator mediator, ICurrentUserService currentUserService)
        {
            _mediator = mediator;
            _currentUserService = currentUserService;
        }

        [HttpPost("ledger")]
        public async Task<IActionResult> GetLedger([FromBody] SupplierLedgerRequestDto request)
        {
            var result = await _mediator.Send(new GetSupplierLedgerQuery(request));
            return Ok(result);
        }

        [HttpPost("payment-entry")]
        public async Task<IActionResult> RecordPayment([FromBody] SupplierPaymentDto paymentDto)
        {
            try 
            {
                // 🚀 SMART INJECTION: Get CompanyId and BranchId via ICurrentUserService (prioritizing request headers)
                if (_currentUserService.CompanyId.HasValue)
                {
                    paymentDto.CompanyId = _currentUserService.CompanyId.Value;
                }
                if (!string.IsNullOrEmpty(_currentUserService.BranchId))
                {
                    paymentDto.BranchId = _currentUserService.BranchId;
                }

                var command = new RecordSupplierPaymentCommand(paymentDto);
                var id = await _mediator.Send(command);
                return Ok(new { Id = id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FinanceController] ERROR in payment-entry: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"[Inner] {ex.InnerException.Message}");
                Console.WriteLine(ex.StackTrace);
                throw; // Rethrow to let Middleware handle the response
            }
        }

        [HttpPost("purchase-entry")]
        public async Task<IActionResult> RecordPurchase([FromBody] SupplierPurchaseDto purchase)
        {
            // 🚀 SMART INJECTION: Get CompanyId and BranchId via ICurrentUserService (prioritizing request headers)
            if (_currentUserService.CompanyId.HasValue)
            {
                purchase.CompanyId = _currentUserService.CompanyId.Value;
            }
            if (!string.IsNullOrEmpty(_currentUserService.BranchId))
            {
                purchase.BranchId = _currentUserService.BranchId;
            }

            var command = new RecordSupplierPurchaseCommand(purchase);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("purchase-return-entry")]
        public async Task<IActionResult> RecordPurchaseReturn([FromBody] SupplierPurchaseDto purchaseReturn)
        {
            // 🚀 SMART INJECTION: Get CompanyId and BranchId via ICurrentUserService (prioritizing request headers)
            if (_currentUserService.CompanyId.HasValue)
            {
                purchaseReturn.CompanyId = _currentUserService.CompanyId.Value;
            }
            if (!string.IsNullOrEmpty(_currentUserService.BranchId))
            {
                purchaseReturn.BranchId = _currentUserService.BranchId;
            }

            // Force TransactionType to DebitNote for this endpoint
            purchaseReturn.TransactionType = "DebitNote";

            var command = new RecordSupplierPurchaseCommand(purchaseReturn);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpGet("pending-dues")]
        public async Task<IActionResult> GetPendingDues([FromQuery] string? branchId = null, [FromQuery] string? companyId = null)
        {
            var result = await _mediator.Send(new GetPendingDuesQuery(branchId, companyId));
            return Ok(result);
        }

        [HttpGet("pending-total")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
        public async Task<IActionResult> GetPendingTotal([FromQuery] string? branchId = null, [FromQuery] string? companyId = null)
        {
            var result = await _mediator.Send(new GetTotalPendingDuesQuery(branchId, companyId));
            return Ok(new { TotalPending = result });
        }

        [HttpPost("total-payments")]
        public async Task<IActionResult> GetTotalPayments([FromBody] DateRangeDto dateRange)
        {
            // If branchId is not in body, try headers
            if (string.IsNullOrEmpty(dateRange.BranchId)) {
                dateRange.BranchId = Request.Headers["X-Branch-Id"].ToString();
            }
            var totalPayments = await _mediator.Send(new GetTotalPaymentsQuery(dateRange));
            return Ok(new { TotalPayments = totalPayments });
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

        [HttpPost("get-grn-statuses")]
        public async Task<IActionResult> GetGRNStatuses([FromBody] List<string> grnNumbers)
        {
            var result = await _mediator.Send(new GetGRNPaymentStatusesQuery(grnNumbers));
            return Ok(result);
        }

        [HttpPost("get-balances")]
        public async Task<IActionResult> GetSupplierBalances([FromBody] List<Guid> supplierIds)
        {
            var result = await _mediator.Send(new GetSupplierBalancesQuery(supplierIds));
            return Ok(result);
        }

        [HttpPost("payments-report")]
        public async Task<IActionResult> GetPaymentsReport([FromBody] PaymentReportRequestDto request)
        {
            var result = await _mediator.Send(new GetPaymentsReportQuery(request));
            return Ok(result);
        }

        [HttpGet("monthly-payments")]
        public async Task<IActionResult> GetMonthlyPayments([FromQuery] int months = 6, [FromQuery] string? branchId = null)
        {
            var result = await _mediator.Send(new GetMonthlyPaymentsTrendQuery(months, branchId));
            return Ok(result);
        }

        [HttpDelete("payment/{id}")]
        public async Task<IActionResult> DeletePayment(Guid id)
        {
            var command = new DeleteSupplierPaymentCommand(id);
            var result = await _mediator.Send(command);
            if (!result) return NotFound(new { message = "Payment not found or delete failed." });
            return Ok(new { message = "Payment deleted and running balances recalculated successfully." });
        }
    }
}

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

namespace Suppliers.API.Controllers
{
    [Route("api/finance")]
    [ApiController]
    public class FinanceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FinanceController(IMediator mediator)
        {
            _mediator = mediator;
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
                // 🚀 SMART INJECTION: Get CompanyId from Claims
                var companyIdClaim = User.FindFirst("CompanyId")?.Value;
                if (Guid.TryParse(companyIdClaim, out var companyId))
                {
                    paymentDto.CompanyId = companyId;
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
            // 🚀 SMART INJECTION: Get CompanyId from Claims
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (Guid.TryParse(companyIdClaim, out var companyId))
            {
                purchase.CompanyId = companyId;
            }

            var command = new RecordSupplierPurchaseCommand(purchase);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpGet("pending-dues")]
        public async Task<IActionResult> GetPendingDues()
        {
            var result = await _mediator.Send(new GetPendingDuesQuery());
            return Ok(result);
        }

        [HttpGet("pending-total")]
        [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin")]
        public async Task<IActionResult> GetPendingTotal()
        {
            var total = await _mediator.Send(new GetTotalPendingDuesQuery());
            return Ok(new { TotalPending = total });
        }

        [HttpPost("total-payments")]
        public async Task<IActionResult> GetTotalPayments([FromBody] DateRangeDto dateRange)
        {
            var totalPayments = await _mediator.Send(new GetTotalPaymentsQuery(dateRange));
            return Ok(new { TotalPayments = totalPayments });
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
        public async Task<IActionResult> GetMonthlyPayments([FromQuery] int months = 6)
        {
            var result = await _mediator.Send(new GetMonthlyPaymentsTrendQuery(months));
            return Ok(result);
        }
    }
}

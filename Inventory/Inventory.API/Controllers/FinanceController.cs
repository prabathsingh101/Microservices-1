using Inventory.Application.Clients;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Inventory.API.Controllers
{
    [ApiController]
    [Route("api/finance")]
    public sealed class FinanceController : ControllerBase
    {
        private readonly ISupplierClient _supplierClient;
        private readonly ICustomerClient _customerClient;

        public FinanceController(ISupplierClient supplierClient, ICustomerClient customerClient)
        {
            _supplierClient = supplierClient;
            _customerClient = customerClient;
        }

        [HttpPost("supplier-payment")]
        public async Task<IActionResult> RecordSupplierPayment([FromBody] SupplierPaymentRequestDto request)
        {
            if (request == null) return BadRequest("Invalid request body");

            var result = await _supplierClient.RecordPaymentAsync(
                request.SupplierId,
                request.Amount,
                request.ReferenceNumber ?? string.Empty,
                request.Remarks ?? string.Empty,
                request.PaymentMode ?? "Cash",
                request.CreatedBy ?? "Admin",
                request.TransactionType ?? "Payment"
            );

            return Ok(new { success = result, status = "queued" });
        }

        [HttpPost("customer-receipt")]
        public async Task<IActionResult> RecordCustomerReceipt([FromBody] CustomerReceiptRequestDto request)
        {
            if (request == null) return BadRequest("Invalid request body");

            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            var companyIdHeader = Request.Headers["X-Company-Id"].ToString();
            
            if (Guid.TryParse(companyIdHeader, out var companyId) || Guid.TryParse(companyIdClaim, out companyId))
            {
                request.CompanyId = companyId;
            }

            var branchIdClaim = User.FindFirst("BranchId")?.Value;
            var branchIdHeader = Request.Headers["X-Branch-Id"].ToString();
            
            var finalBranchId = !string.IsNullOrEmpty(branchIdHeader) && branchIdHeader != "null" 
                ? branchIdHeader 
                : (!string.IsNullOrEmpty(branchIdClaim) ? branchIdClaim : null);

            if (!string.IsNullOrEmpty(finalBranchId))
            {
                request.BranchId = finalBranchId;
            }

            await _customerClient.RecordReceiptAsync(
                request.CustomerId,
                request.Amount,
                request.PaymentMode ?? "Cash",
                request.ReferenceNumber ?? string.Empty,
                request.Remarks ?? string.Empty,
                request.CreatedBy ?? "Admin",
                request.BranchId,
                request.CompanyId
            );

            return Ok(new { success = true, status = "queued" });
        }
    }

    public class SupplierPaymentRequestDto
    {
        public Guid SupplierId { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMode { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }
        public Guid? CompanyId { get; set; }
        public string? BranchId { get; set; }
        public string? TransactionType { get; set; }
    }

    public class CustomerReceiptRequestDto
    {
        public Guid? CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMode { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }
        public string? BranchId { get; set; }
        public Guid? CompanyId { get; set; }
    }
}

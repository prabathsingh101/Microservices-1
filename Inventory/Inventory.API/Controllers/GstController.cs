using Inventory.Application.Gst.Services;
using Inventory.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Inventory.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GstController : ControllerBase
    {
        private readonly IGstService _gstService;
        private readonly IInventoryDbContext _context;

        public GstController(IGstService gstService, IInventoryDbContext context)
        {
            _gstService = gstService;
            _context = context;
        }

        [HttpGet("gstr1")]
        [Authorize(Roles = "Super Admin, Admin, Manager, User")]
        public async Task<IActionResult> GetGstr1([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var companyId = ResolveCompanyId();
            if (companyId == Guid.Empty)
            {
                return BadRequest("CompanyId is required and could not be resolved.");
            }

            try
            {
                var fileBytes = await _gstService.GenerateGstr1ExcelAsync(startDate, endDate, companyId);
                string fileName = $"GSTR1_{startDate:yyyyMMdd}_to_{endDate:yyyyMMdd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating GSTR-1: {ex.Message}");
            }
        }

        [HttpGet("gstr3b")]
        [Authorize(Roles = "Super Admin, Admin, Manager, User")]
        public async Task<IActionResult> GetGstr3b([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var companyId = ResolveCompanyId();
            if (companyId == Guid.Empty)
            {
                return BadRequest("CompanyId is required and could not be resolved.");
            }

            try
            {
                var summary = await _gstService.GetGstr3bSummaryAsync(startDate, endDate, companyId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error compiling GSTR-3B: {ex.Message}");
            }
        }

        [HttpPost("reconcile")]
        [Authorize(Roles = "Super Admin, Admin, Manager, User")]
        public async Task<IActionResult> ReconcileGstr2b(
            [FromForm] IFormFile file, 
            [FromQuery] DateTime startDate, 
            [FromQuery] DateTime endDate)
        {
            var companyId = ResolveCompanyId();
            if (companyId == Guid.Empty)
            {
                return BadRequest("CompanyId is required and could not be resolved.");
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;
                    
                    var result = await _gstService.ReconcileGstr2bAsync(stream, file.FileName, startDate, endDate, companyId);
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error executing reconciliation: {ex.Message}");
            }
        }

        [HttpGet("rcm-ledger")]
        [Authorize(Roles = "Super Admin, Admin, Manager, User")]
        public async Task<IActionResult> GetRcmLedger([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var companyId = ResolveCompanyId();
            if (companyId == Guid.Empty)
            {
                return BadRequest("CompanyId is required.");
            }

            try
            {
                // Query Purchase Orders that attract RCM
                var posQuery = _context.PurchaseOrders
                    .Where(p => p.CompanyId == companyId && (p.IsRcm || string.IsNullOrEmpty(p.SupplierGstIn) || p.SupplierGstIn == "URP"));

                if (startDate.HasValue) posQuery = posQuery.Where(p => p.PoDate >= startDate.Value);
                if (endDate.HasValue) posQuery = posQuery.Where(p => p.PoDate <= endDate.Value);

                var rcmPos = await posQuery.Select(p => new 
                {
                    Id = p.Id,
                    Source = "Purchase",
                    VoucherNo = p.PoNumber,
                    Date = p.PoDate,
                    SupplierName = p.SupplierName ?? "Unregistered Supplier",
                    SupplierGstin = p.SupplierGstIn ?? "URP",
                    TaxableValue = p.SubTotal,
                    GstRate = p.RcmGstRate ?? 18,
                    CgstAmount = p.RcmCgstAmount ?? (p.IsRcm ? (p.TotalTax / 2) : 0),
                    SgstAmount = p.RcmSgstAmount ?? (p.IsRcm ? (p.TotalTax / 2) : 0),
                    IgstAmount = p.RcmIgstAmount ?? 0,
                    TotalTax = p.RcmTaxAmount ?? (p.IsRcm ? p.TotalTax : 0),
                    Paid = p.RcmPaid,
                    PaidDate = p.RcmPaidDate
                }).ToListAsync();

                // Query Expenses that attract RCM
                var expQuery = _context.ExpenseEntries
                    .Include(e => e.Category)
                    .Where(e => e.CompanyId == companyId && e.IsRcm);

                if (startDate.HasValue) expQuery = expQuery.Where(e => e.ExpenseDate >= startDate.Value);
                if (endDate.HasValue) expQuery = expQuery.Where(e => e.ExpenseDate <= endDate.Value);

                var rcmExps = await expQuery.Select(e => new 
                {
                    Id = e.Id,
                    Source = "Expense",
                    VoucherNo = e.ReferenceNo ?? ("EXP-" + e.Id.ToString().Substring(0, 8).ToUpper()),
                    Date = e.ExpenseDate,
                    SupplierName = e.SupplierName ?? (e.Category != null ? e.Category.Name : "Service Provider"),
                    SupplierGstin = e.SupplierGstin ?? "URP",
                    TaxableValue = e.RcmTaxableValue ?? e.Amount,
                    GstRate = e.RcmGstRate ?? 18,
                    CgstAmount = e.RcmCgstAmount ?? 0,
                    SgstAmount = e.RcmSgstAmount ?? 0,
                    IgstAmount = e.RcmIgstAmount ?? e.RcmTaxAmount ?? 0,
                    TotalTax = e.RcmTaxAmount ?? 0,
                    Paid = e.IsRcm && e.RcmPaid,
                    PaidDate = e.RcmPaidDate
                }).ToListAsync();

                // Combine both lists
                var ledger = new List<object>();
                
                foreach (var p in rcmPos)
                {
                    decimal cgst = p.CgstAmount;
                    decimal sgst = p.SgstAmount;
                    decimal igst = p.IgstAmount;
                    decimal totalTax = p.TotalTax;

                    if (totalTax == 0 && p.TaxableValue > 0)
                    {
                        totalTax = p.TaxableValue * 0.18m;
                        cgst = totalTax / 2;
                        sgst = totalTax / 2;
                    }

                    ledger.Add(new
                    {
                        p.Id,
                        p.Source,
                        p.VoucherNo,
                        p.Date,
                        p.SupplierName,
                        p.SupplierGstin,
                        p.TaxableValue,
                        GstRate = p.GstRate,
                        CgstAmount = cgst,
                        SgstAmount = sgst,
                        IgstAmount = igst,
                        TotalTax = totalTax,
                        p.Paid,
                        p.PaidDate
                    });
                }

                foreach (var e in rcmExps)
                {
                    decimal cgst = e.CgstAmount;
                    decimal sgst = e.SgstAmount;
                    decimal igst = e.IgstAmount;
                    decimal totalTax = e.TotalTax;

                    if (totalTax == 0 && e.TaxableValue > 0)
                    {
                        decimal rate = e.GstRate;
                        totalTax = e.TaxableValue * (rate / 100m);
                        cgst = totalTax / 2;
                        sgst = totalTax / 2;
                    }

                    ledger.Add(new
                    {
                        e.Id,
                        e.Source,
                        e.VoucherNo,
                        e.Date,
                        e.SupplierName,
                        e.SupplierGstin,
                        e.TaxableValue,
                        GstRate = e.GstRate,
                        CgstAmount = cgst,
                        SgstAmount = sgst,
                        IgstAmount = igst,
                        TotalTax = totalTax,
                        e.Paid,
                        e.PaidDate
                    });
                }

                var sortedLedger = ledger.OrderByDescending(x => ((dynamic)x).Date).ToList();
                return Ok(sortedLedger);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching RCM ledger: {ex.Message}");
            }
        }

        [HttpPost("rcm-pay")]
        [Authorize(Roles = "Super Admin, Admin, Manager, User")]
        public async Task<IActionResult> PayRcm([FromBody] RcmPayRequest request)
        {
            if (request == null || request.Ids == null || request.Ids.Count == 0)
            {
                return BadRequest("No transaction IDs provided.");
            }

            try
            {
                var posToUpdate = await _context.PurchaseOrders
                    .Where(p => request.Ids.Contains(p.Id))
                    .ToListAsync();

                foreach (var po in posToUpdate)
                {
                    po.RcmPaid = true;
                    po.RcmPaidDate = DateTime.Now;
                }

                var expsToUpdate = await _context.ExpenseEntries
                    .Where(e => request.Ids.Contains(e.Id))
                    .ToListAsync();

                foreach (var exp in expsToUpdate)
                {
                    exp.RcmPaid = true;
                    exp.RcmPaidDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "RCM payments recorded successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error recording RCM payments: {ex.Message}");
            }
        }

        private Guid ResolveCompanyId()
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            var companyIdHeader = Request.Headers["X-Company-Id"].ToString();

            if (Guid.TryParse(companyIdHeader, out var companyId) || Guid.TryParse(companyIdClaim, out companyId))
            {
                return companyId;
            }

            return Guid.Empty;
        }
    }

    public class RcmPayRequest
    {
        public List<Guid> Ids { get; set; } = new List<Guid>();
    }
}

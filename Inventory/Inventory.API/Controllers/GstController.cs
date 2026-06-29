using Inventory.Application.Gst.Services;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Services;
using Inventory.Application.Clients;
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
        private readonly IPdfService _pdfService;
        private readonly ICompanyClient _companyClient;
        private readonly ICustomerClient _customerClient;

        public GstController(
            IGstService gstService, 
            IInventoryDbContext context, 
            IPdfService pdfService, 
            ICompanyClient companyClient,
            ICustomerClient customerClient)
        {
            _gstService = gstService;
            _context = context;
            _pdfService = pdfService;
            _companyClient = companyClient;
            _customerClient = customerClient;
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

        [HttpGet("gstr3b/pdf")]
        [Authorize(Roles = "Super Admin, Admin, Manager, User")]
        public async Task<IActionResult> GetGstr3bPdf([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var companyId = ResolveCompanyId();
            if (companyId == Guid.Empty)
            {
                return BadRequest("CompanyId is required and could not be resolved.");
            }

            try
            {
                var summary = await _gstService.GetGstr3bSummaryAsync(startDate, endDate, companyId);
                var companyProfile = await _companyClient.GetCompanyProfileAsync();

                var companyName = companyProfile?.Name ?? "Subham Electronics";
                var companyGstin = companyProfile?.Gstin ?? "URP";
                var period = $"{startDate:dd-MM-yyyy} to {endDate:dd-MM-yyyy}";

                var outputTax = summary.OutwardSupplies.CentralTax + summary.OutwardSupplies.StateTax + summary.OutwardSupplies.IntegratedTax;
                var inputTax = summary.InputTaxCredit.CentralTax + summary.InputTaxCredit.StateTax + summary.InputTaxCredit.IntegratedTax;
                var netPayable = summary.NetPayable.CentralTax + summary.NetPayable.StateTax + summary.NetPayable.IntegratedTax;

                var utilizedIgst = Math.Min(summary.OutwardSupplies.IntegratedTax, summary.InputTaxCredit.IntegratedTax);
                var utilizedCgst = Math.Min(summary.OutwardSupplies.CentralTax, summary.InputTaxCredit.CentralTax);
                var utilizedSgst = Math.Min(summary.OutwardSupplies.StateTax, summary.InputTaxCredit.StateTax);

                var payableIgst = summary.NetPayable.IntegratedTax;
                var payableCgst = summary.NetPayable.CentralTax;
                var payableSgst = summary.NetPayable.StateTax;

                var html = $@"
<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<style>
    body {{ font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; color: #333; margin: 30px; font-size: 13px; line-height: 1.4; }}
    .header-container {{ display: table; width: 100%; border-bottom: 2px solid #2563eb; padding-bottom: 15px; margin-bottom: 20px; }}
    .header-left {{ display: table-cell; width: 60%; }}
    .header-right {{ display: table-cell; width: 40%; text-align: right; vertical-align: bottom; }}
    .company-name {{ font-size: 22px; font-weight: bold; color: #1e3a8a; margin: 0 0 5px 0; text-transform: uppercase; }}
    .gstin {{ font-size: 13px; font-weight: bold; color: #475569; margin: 0; }}
    .title {{ font-size: 18px; font-weight: bold; color: #2563eb; margin: 0 0 5px 0; text-transform: uppercase; }}
    .period {{ font-size: 12px; color: #64748b; margin: 0; }}
    
    .cards-container {{ display: table; width: 100%; margin-bottom: 25px; border-collapse: separate; border-spacing: 12px 0; }}
    .card {{ display: table-cell; width: 33.33%; padding: 15px; border-radius: 8px; box-sizing: border-box; }}
    .card-output {{ background-color: #fef2f2; border: 1px solid #fee2e2; }}
    .card-input {{ background-color: #f0fdf4; border: 1px solid #dcfce7; }}
    .card-payable {{ background-color: #eff6ff; border: 1px solid #dbeafe; }}
    .card-title {{ font-size: 11px; font-weight: bold; color: #64748b; text-transform: uppercase; margin-bottom: 5px; }}
    .card-value {{ font-size: 18px; font-weight: bold; margin-bottom: 3px; }}
    .card-value.red {{ color: #dc2626; }}
    .card-value.green {{ color: #16a34a; }}
    .card-value.blue {{ color: #2563eb; }}
    .card-sub {{ font-size: 10px; color: #94a3b8; }}

    .section-title {{ font-size: 14px; font-weight: bold; color: #1e293b; margin: 20px 0 10px 0; border-bottom: 1px solid #e2e8f0; padding-bottom: 5px; }}
    table {{ width: 100%; border-collapse: collapse; margin-bottom: 20px; font-size: 12px; }}
    th {{ background-color: #f8fafc; color: #475569; font-weight: bold; text-align: left; padding: 10px 8px; border-bottom: 2px solid #cbd5e1; border-top: 1px solid #e2e8f0; }}
    td {{ padding: 10px 8px; border-bottom: 1px solid #e2e8f0; color: #334155; }}
    .text-right {{ text-align: right; }}
    .font-bold {{ font-weight: bold; }}
    .bg-light {{ background-color: #f8fafc; }}
    
    .footer {{ text-align: center; font-size: 10px; color: #94a3b8; margin-top: 50px; border-top: 1px solid #e2e8f0; padding-top: 10px; }}
</style>
</head>
<body>
    <div class=""header-container"">
        <div class=""header-left"">
            <h1 class=""company-name"">{companyName}</h1>
            <p class=""gstin"">GSTIN: {companyGstin}</p>
        </div>
        <div class=""header-right"">
            <h2 class=""title"">GSTR-3B Compliant Summary</h2>
            <p class=""period"">Period: {period}</p>
        </div>
    </div>

    <div class=""cards-container"">
        <div class=""card card-output"">
            <div class=""card-title"">Output Tax Liability</div>
            <div class=""card-value red"">₹{outputTax:N2}</div>
            <div class=""card-sub"">From Outward Supplies (Sales)</div>
        </div>
        <div class=""card card-input"">
            <div class=""card-title"">Eligible Input Tax Credit (ITC)</div>
            <div class=""card-value green"">₹{inputTax:N2}</div>
            <div class=""card-sub"">From Inward Purchases</div>
        </div>
        <div class=""card card-payable"">
            <div class=""card-title"">Net Tax Payable in Cash</div>
            <div class=""card-value blue"">₹{netPayable:N2}</div>
            <div class=""card-sub"">Liability minus available credit</div>
        </div>
    </div>

    <div class=""section-title"">Table 3.1: Details of Outward Supplies & Inward Supplies Liable to Reverse Charge</div>
    <table>
        <thead>
            <tr>
                <th>Nature of Supplies</th>
                <th class=""text-right"">Total Taxable Value</th>
                <th class=""text-right"">IGST (Integrated)</th>
                <th class=""text-right"">CGST (Central)</th>
                <th class=""text-right"">SGST (State)</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td class=""font-bold"">(a) Outward taxable supplies (other than zero rated, nil rated and exempted)</td>
                <td class=""text-right"">₹{summary.OutwardSupplies.TaxableValue:N2}</td>
                <td class=""text-right"">₹{summary.OutwardSupplies.IntegratedTax:N2}</td>
                <td class=""text-right"">₹{summary.OutwardSupplies.CentralTax:N2}</td>
                <td class=""text-right"">₹{summary.OutwardSupplies.StateTax:N2}</td>
            </tr>
            <tr class=""bg-light font-bold"">
                <td>Total Outward Supplies Liability</td>
                <td class=""text-right"">₹{summary.OutwardSupplies.TaxableValue:N2}</td>
                <td class=""text-right"">₹{summary.OutwardSupplies.IntegratedTax:N2}</td>
                <td class=""text-right"">₹{summary.OutwardSupplies.CentralTax:N2}</td>
                <td class=""text-right"">₹{summary.OutwardSupplies.StateTax:N2}</td>
            </tr>
        </tbody>
    </table>

    <div class=""section-title"">Table 4: Details of Eligible Input Tax Credit (ITC)</div>
    <table>
        <thead>
            <tr>
                <th>Details</th>
                <th class=""text-right"">Total Taxable Value</th>
                <th class=""text-right"">IGST (Integrated)</th>
                <th class=""text-right"">CGST (Central)</th>
                <th class=""text-right"">SGST (State)</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td class=""font-bold"">(A) (5) All other ITC (from local & interstate purchases)</td>
                <td class=""text-right"">₹{summary.InputTaxCredit.TaxableValue:N2}</td>
                <td class=""text-right"">₹{summary.InputTaxCredit.IntegratedTax:N2}</td>
                <td class=""text-right"">₹{summary.InputTaxCredit.CentralTax:N2}</td>
                <td class=""text-right"">₹{summary.InputTaxCredit.StateTax:N2}</td>
            </tr>
            <tr class=""bg-light font-bold"">
                <td>Total Eligible Input Tax Credit</td>
                <td class=""text-right"">₹{summary.InputTaxCredit.TaxableValue:N2}</td>
                <td class=""text-right"">₹{summary.InputTaxCredit.IntegratedTax:N2}</td>
                <td class=""text-right"">₹{summary.InputTaxCredit.CentralTax:N2}</td>
                <td class=""text-right"">₹{summary.InputTaxCredit.StateTax:N2}</td>
            </tr>
        </tbody>
    </table>

    <div class=""section-title"">Table 6.1: Payment of Tax (Net cash payable after ITC utilization)</div>
    <table>
        <thead>
            <tr>
                <th>Tax Description</th>
                <th class=""text-right"">Total Tax Payable</th>
                <th class=""text-right"">Paid Through ITC</th>
                <th class=""text-right"">Paid In Cash (Net Payable)</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td class=""font-bold"">Integrated Tax (IGST)</td>
                <td class=""text-right"">₹{summary.OutwardSupplies.IntegratedTax:N2}</td>
                <td class=""text-right"">₹{utilizedIgst:N2}</td>
                <td class=""text-right font-bold"" style=""color: #2563eb;"">₹{payableIgst:N2}</td>
            </tr>
            <tr>
                <td class=""font-bold"">Central Tax (CGST)</td>
                <td class=""text-right"">₹{summary.OutwardSupplies.CentralTax:N2}</td>
                <td class=""text-right"">₹{utilizedCgst:N2}</td>
                <td class=""text-right font-bold"" style=""color: #2563eb;"">₹{payableCgst:N2}</td>
            </tr>
            <tr>
                <td class=""font-bold"">State Tax (SGST)</td>
                <td class=""text-right"">₹{summary.OutwardSupplies.StateTax:N2}</td>
                <td class=""text-right"">₹{utilizedSgst:N2}</td>
                <td class=""text-right font-bold"" style=""color: #2563eb;"">₹{payableSgst:N2}</td>
            </tr>
        </tbody>
    </table>

    <div class=""footer"">
        <p>Report Generated on {DateTime.Now:dd-MMM-yyyy HH:mm} | Powered by Subham Electronics Retail POS</p>
    </div>
</body>
</html>";

                var pdfBytes = _pdfService.Convert(html);
                string fileName = $"GSTR3B_{startDate:yyyyMMdd}_to_{endDate:yyyyMMdd}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating GSTR-3B PDF: {ex.Message}");
            }
        }

        [HttpGet("gstr1/pdf")]
        [Authorize(Roles = "Super Admin, Admin, Manager, User")]
        public async Task<IActionResult> GetGstr1Pdf([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var companyId = ResolveCompanyId();
            if (companyId == Guid.Empty)
            {
                return BadRequest("CompanyId is required.");
            }

            try
            {
                var adjustedEndDate = endDate.Date.AddDays(1).AddTicks(-1);
                var invoices = await _context.SalesInvoices
                    .Include(i => i.Items)
                    .Where(i => i.CompanyId == companyId && i.InvoiceDate >= startDate.Date && i.InvoiceDate <= adjustedEndDate && i.Status != "Cancelled")
                    .ToListAsync();

                var companyProfile = await _companyClient.GetCompanyProfileAsync();
                var companyName = companyProfile?.Name ?? "Subham Electronics";
                var companyGstin = companyProfile?.Gstin ?? "URP";
                var period = $"{startDate:dd-MM-yyyy} to {endDate:dd-MM-yyyy}";

                // Fetch registered customer details for fallback if CustomerGstIn is not populated
                var customerIds = invoices
                    .Where(i => i.CustomerId.HasValue && string.IsNullOrEmpty(i.CustomerGstIn))
                    .Select(i => i.CustomerId!.Value)
                    .Distinct()
                    .ToList();

                var customerDetails = customerIds.Any()
                    ? await _customerClient.GetCustomerDetailsByIdsAsync(customerIds)
                    : new Dictionary<Guid, CustomerLookupDto>();

                // Split into B2B and B2C
                var b2bInvoices = new List<dynamic>();
                var b2cInvoices = new List<dynamic>();

                decimal totalSalesValue = 0;
                decimal totalTaxableValue = 0;
                decimal totalIgst = 0;
                decimal totalCgst = 0;
                decimal totalSgst = 0;

                foreach (var inv in invoices)
                {
                    totalSalesValue += inv.GrandTotal;
                    totalTaxableValue += inv.SubTotal;
                    totalIgst += inv.IgstAmount ?? 0;
                    totalCgst += inv.CgstAmount ?? 0;
                    totalSgst += inv.SgstAmount ?? 0;

                    string? gstin = !string.IsNullOrEmpty(inv.CustomerGstIn) ? inv.CustomerGstIn : null;
                    string? name = !string.IsNullOrEmpty(inv.CustomerName) ? inv.CustomerName : null;

                    if (gstin == null && inv.CustomerId.HasValue && customerDetails.TryGetValue(inv.CustomerId.Value, out var c))
                    {
                        gstin = c.GstNumber;
                        name = c.CustomerName;
                    }

                    var invoiceData = new {
                        InvoiceNo = inv.InvoiceNo,
                        InvoiceDate = inv.InvoiceDate,
                        CustomerName = name ?? "Cash/Unregistered",
                        CustomerGstin = gstin ?? "Consumer",
                        TaxableValue = inv.SubTotal,
                        Igst = inv.IgstAmount ?? 0,
                        Cgst = inv.CgstAmount ?? 0,
                        Sgst = inv.SgstAmount ?? 0,
                        TotalValue = inv.GrandTotal
                    };

                    if (!string.IsNullOrWhiteSpace(gstin))
                    {
                        b2bInvoices.Add(invoiceData);
                    }
                    else
                    {
                        b2cInvoices.Add(invoiceData);
                    }
                }

                var html = $@"
<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<style>
    body {{ font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; color: #333; margin: 30px; font-size: 11px; line-height: 1.4; }}
    .header-container {{ display: table; width: 100%; border-bottom: 2px solid #2563eb; padding-bottom: 15px; margin-bottom: 20px; }}
    .header-left {{ display: table-cell; width: 60%; }}
    .header-right {{ display: table-cell; width: 40%; text-align: right; vertical-align: bottom; }}
    .company-name {{ font-size: 20px; font-weight: bold; color: #1e3a8a; margin: 0 0 5px 0; text-transform: uppercase; }}
    .gstin {{ font-size: 12px; font-weight: bold; color: #475569; margin: 0; }}
    .title {{ font-size: 16px; font-weight: bold; color: #2563eb; margin: 0 0 5px 0; text-transform: uppercase; }}
    .period {{ font-size: 11px; color: #64748b; margin: 0; }}
    
    .cards-container {{ display: table; width: 100%; margin-bottom: 25px; border-collapse: separate; border-spacing: 12px 0; }}
    .card {{ display: table-cell; width: 25%; padding: 12px; border-radius: 6px; box-sizing: border-box; background-color: #f8fafc; border: 1px solid #e2e8f0; }}
    .card-title {{ font-size: 9px; font-weight: bold; color: #64748b; text-transform: uppercase; margin-bottom: 4px; }}
    .card-value {{ font-size: 15px; font-weight: bold; color: #0f172a; }}

    .section-title {{ font-size: 12px; font-weight: bold; color: #1e293b; margin: 20px 0 10px 0; border-bottom: 1px solid #cbd5e1; padding-bottom: 4px; }}
    table {{ width: 100%; border-collapse: collapse; margin-bottom: 20px; font-size: 10px; }}
    th {{ background-color: #f1f5f9; color: #334155; font-weight: bold; text-align: left; padding: 6px 8px; border-bottom: 2px solid #cbd5e1; }}
    td {{ padding: 6px 8px; border-bottom: 1px solid #e2e8f0; color: #334155; }}
    .text-right {{ text-align: right; }}
    .font-bold {{ font-weight: bold; }}
    .bg-light {{ background-color: #f8fafc; }}
    
    .footer {{ text-align: center; font-size: 9px; color: #94a3b8; margin-top: 50px; border-top: 1px solid #e2e8f0; padding-top: 10px; }}
</style>
</head>
<body>
    <div class=""header-container"">
        <div class=""header-left"">
            <h1 class=""company-name"">{companyName}</h1>
            <p class=""gstin"">GSTIN: {companyGstin}</p>
        </div>
        <div class=""header-right"">
            <h2 class=""title"">GSTR-1 Sales Report</h2>
            <p class=""period"">Period: {period}</p>
        </div>
    </div>

    <div class=""cards-container"">
        <div class=""card"">
            <div class=""card-title"">Total Sales Value</div>
            <div class=""card-value"">₹{totalSalesValue:N2}</div>
        </div>
        <div class=""card"">
            <div class=""card-title"">Total Taxable Value</div>
            <div class=""card-value"">₹{totalTaxableValue:N2}</div>
        </div>
        <div class=""card"">
            <div class=""card-title"">Total IGST</div>
            <div class=""card-value"">₹{totalIgst:N2}</div>
        </div>
        <div class=""card"">
            <div class=""card-title"">Total CGST + SGST</div>
            <div class=""card-value"">₹{totalCgst + totalSgst:N2}</div>
        </div>
    </div>

    <div class=""section-title"">Section 1: B2B Invoices (Registered Customers)</div>
    <table>
        <thead>
            <tr>
                <th>Invoice No</th>
                <th>Date</th>
                <th>Customer Name</th>
                <th>Customer GSTIN</th>
                <th class=""text-right"">Taxable Value</th>
                <th class=""text-right"">CGST</th>
                <th class=""text-right"">SGST</th>
                <th class=""text-right"">IGST</th>
                <th class=""text-right"">Total Value</th>
            </tr>
        </thead>
        <tbody>";

                if (b2bInvoices.Count == 0)
                {
                    html += "<tr><td colspan=\"9\" style=\"text-align: center; color: #94a3b8;\">No B2B invoices found in this period.</td></tr>";
                }
                else
                {
                    foreach (var inv in b2bInvoices)
                    {
                        html += $@"
            <tr>
                <td>{inv.InvoiceNo}</td>
                <td>{inv.InvoiceDate:dd-MM-yyyy}</td>
                <td>{inv.CustomerName}</td>
                <td><code>{inv.CustomerGstin}</code></td>
                <td class=""text-right"">₹{inv.TaxableValue:N2}</td>
                <td class=""text-right"">₹{inv.Cgst:N2}</td>
                <td class=""text-right"">₹{inv.Sgst:N2}</td>
                <td class=""text-right"">₹{inv.Igst:N2}</td>
                <td class=""text-right font-bold"">₹{inv.TotalValue:N2}</td>
            </tr>";
                    }
                }

                html += $@"
        </tbody>
    </table>

    <div class=""section-title"">Section 2: B2C Invoices (Unregistered Customers & Consumers)</div>
    <table>
        <thead>
            <tr>
                <th>Invoice No</th>
                <th>Date</th>
                <th>Customer Name</th>
                <th class=""text-right"">Taxable Value</th>
                <th class=""text-right"">CGST</th>
                <th class=""text-right"">SGST</th>
                <th class=""text-right"">IGST</th>
                <th class=""text-right"">Total Value</th>
            </tr>
        </thead>
        <tbody>";

                if (b2cInvoices.Count == 0)
                {
                    html += "<tr><td colspan=\"8\" style=\"text-align: center; color: #94a3b8;\">No B2C invoices found in this period.</td></tr>";
                }
                else
                {
                    foreach (var inv in b2cInvoices)
                    {
                        html += $@"
            <tr>
                <td>{inv.InvoiceNo}</td>
                <td>{inv.InvoiceDate:dd-MM-yyyy}</td>
                <td>{inv.CustomerName}</td>
                <td class=""text-right"">₹{inv.TaxableValue:N2}</td>
                <td class=""text-right"">₹{inv.Cgst:N2}</td>
                <td class=""text-right"">₹{inv.Sgst:N2}</td>
                <td class=""text-right"">₹{inv.Igst:N2}</td>
                <td class=""text-right font-bold"">₹{inv.TotalValue:N2}</td>
            </tr>";
                    }
                }

                html += $@"
        </tbody>
    </table>

    <div class=""footer"">
        <p>Report Generated on {DateTime.Now:dd-MMM-yyyy HH:mm} | Powered by Subham Electronics Retail POS</p>
    </div>
</body>
</html>";

                var pdfBytes = _pdfService.Convert(html);
                string fileName = $"GSTR1_Report_{startDate:yyyyMMdd}_to_{endDate:yyyyMMdd}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating GSTR-1 PDF: {ex.Message}");
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

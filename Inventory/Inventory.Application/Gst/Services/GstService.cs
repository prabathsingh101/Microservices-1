using ClosedXML.Excel;
using Inventory.Application.Clients;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Gst.DTOs;
using Inventory.Application.PurchaseReturn;
using Inventory.Domain.Entities;
using Inventory.Domain.Entities.SalesInvoice;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Inventory.Application.Gst.Services
{
    public interface IGstService
    {
        Task<byte[]> GenerateGstr1ExcelAsync(DateTime startDate, DateTime endDate, Guid companyId);
        Task<Gstr3bSummaryDto> GetGstr3bSummaryAsync(DateTime startDate, DateTime endDate, Guid companyId);
        Task<Gstr2bReconResultDto> ReconcileGstr2bAsync(Stream fileStream, string fileName, DateTime startDate, DateTime endDate, Guid companyId);
    }

    public class GstService : IGstService
    {
        private readonly IInventoryDbContext _context;
        private readonly ICustomerClient _customerClient;
        private readonly ISupplierClient _supplierClient;
        private readonly ICompanyClient _companyClient;

        public GstService(
            IInventoryDbContext context,
            ICustomerClient customerClient,
            ISupplierClient supplierClient,
            ICompanyClient companyClient)
        {
            _context = context;
            _customerClient = customerClient;
            _supplierClient = supplierClient;
            _companyClient = companyClient;
        }

        public async Task<byte[]> GenerateGstr1ExcelAsync(DateTime startDate, DateTime endDate, Guid companyId)
        {
            var adjustedEndDate = endDate.Date.AddDays(1).AddTicks(-1);
            var invoices = await _context.SalesInvoices
                .Include(i => i.Items)
                .Where(i => i.CompanyId == companyId && i.InvoiceDate >= startDate.Date && i.InvoiceDate <= adjustedEndDate && i.Status != "Cancelled")
                .ToListAsync();

            // Fetch registered customer details for fallback if CustomerGstIn is not populated
            var customerIds = invoices
                .Where(i => i.CustomerId.HasValue && string.IsNullOrEmpty(i.CustomerGstIn))
                .Select(i => i.CustomerId!.Value)
                .Distinct()
                .ToList();

            var customerDetails = customerIds.Any()
                ? await _customerClient.GetCustomerDetailsByIdsAsync(customerIds)
                : new Dictionary<Guid, CustomerLookupDto>();

            // Get product list for HSN lookup
            var productIds = invoices.SelectMany(i => i.Items).Select(item => item.ProductId).Distinct().ToList();
            var productsDict = await _context.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => new { p.HSNCode, p.Description });

            using (var workbook = new XLWorkbook())
            {
                // --- 1. B2B Sheet ---
                var b2bSheet = workbook.Worksheets.Add("B2B");
                b2bSheet.Cell(1, 1).Value = "GSTIN of Receiver";
                b2bSheet.Cell(1, 2).Value = "Receiver Name";
                b2bSheet.Cell(1, 3).Value = "Invoice Number";
                b2bSheet.Cell(1, 4).Value = "Invoice Date";
                b2bSheet.Cell(1, 5).Value = "Invoice Value";
                b2bSheet.Cell(1, 6).Value = "Place Of Supply";
                b2bSheet.Cell(1, 7).Value = "Reverse Charge";
                b2bSheet.Cell(1, 8).Value = "Invoice Type";
                b2bSheet.Cell(1, 9).Value = "Rate";
                b2bSheet.Cell(1, 10).Value = "Taxable Value";
                b2bSheet.Cell(1, 11).Value = "Cess Amount";

                var headerRow = b2bSheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightCyan;

                int b2bRowIdx = 2;
                foreach (var invoice in invoices)
                {
                    string? gstin = !string.IsNullOrEmpty(invoice.CustomerGstIn) ? invoice.CustomerGstIn : null;
                    string? name = !string.IsNullOrEmpty(invoice.CustomerName) ? invoice.CustomerName : null;

                    if (gstin == null && invoice.CustomerId.HasValue && customerDetails.TryGetValue(invoice.CustomerId.Value, out var c))
                    {
                        gstin = c.GstNumber;
                        name = c.CustomerName;
                    }

                    if (string.IsNullOrWhiteSpace(gstin)) continue; // Not a B2B invoice

                    // Group items by tax rate
                    var rateGroups = invoice.Items.GroupBy(item => item.GSTPercent);
                    foreach (var group in rateGroups)
                    {
                        b2bSheet.Cell(b2bRowIdx, 1).Value = gstin;
                        b2bSheet.Cell(b2bRowIdx, 2).Value = name ?? "Registered Customer";
                        b2bSheet.Cell(b2bRowIdx, 3).Value = invoice.InvoiceNo;
                        b2bSheet.Cell(b2bRowIdx, 4).Value = invoice.InvoiceDate.ToString("dd-MMM-yyyy");
                        b2bSheet.Cell(b2bRowIdx, 5).Value = invoice.GrandTotal;
                        b2bSheet.Cell(b2bRowIdx, 6).Value = GetPlaceOfSupply(gstin, invoice.PlaceOfSupply);
                        b2bSheet.Cell(b2bRowIdx, 7).Value = "N";
                        b2bSheet.Cell(b2bRowIdx, 8).Value = "Regular";
                        b2bSheet.Cell(b2bRowIdx, 9).Value = group.Key;
                        b2bSheet.Cell(b2bRowIdx, 10).Value = group.Sum(item => item.Total - item.TaxAmount);
                        b2bSheet.Cell(b2bRowIdx, 11).Value = 0.00;
                        b2bRowIdx++;
                    }
                }
                b2bSheet.Columns().AdjustToContents();

                // --- 2. B2CS Sheet ---
                var b2csSheet = workbook.Worksheets.Add("B2CS");
                b2csSheet.Cell(1, 1).Value = "Type";
                b2csSheet.Cell(1, 2).Value = "Place Of Supply";
                b2csSheet.Cell(1, 3).Value = "Rate";
                b2csSheet.Cell(1, 4).Value = "Taxable Value";
                b2csSheet.Cell(1, 5).Value = "Cess Amount";

                var b2csHeader = b2csSheet.Row(1);
                b2csHeader.Style.Font.Bold = true;
                b2csHeader.Style.Fill.BackgroundColor = XLColor.LightCyan;

                // Unregistered invoices (B2C)
                var b2cInvoices = invoices.Where(invoice => {
                    string? gstin = !string.IsNullOrEmpty(invoice.CustomerGstIn) ? invoice.CustomerGstIn : null;
                    if (gstin == null && invoice.CustomerId.HasValue && customerDetails.TryGetValue(invoice.CustomerId.Value, out var c))
                    {
                        gstin = c.GstNumber;
                    }
                    return string.IsNullOrWhiteSpace(gstin);
                }).ToList();

                // Group by Place of Supply and Rate
                var b2csGroups = b2cInvoices
                    .SelectMany(inv => inv.Items.Select(item => new {
                        PlaceOfSupply = GetPlaceOfSupply(null, inv.PlaceOfSupply),
                        item.GSTPercent,
                        TaxableValue = item.Total - item.TaxAmount
                    }))
                    .GroupBy(x => new { x.PlaceOfSupply, x.GSTPercent });

                int b2csRowIdx = 2;
                foreach (var group in b2csGroups)
                {
                    b2csSheet.Cell(b2csRowIdx, 1).Value = "OE";
                    b2csSheet.Cell(b2csRowIdx, 2).Value = group.Key.PlaceOfSupply;
                    b2csSheet.Cell(b2csRowIdx, 3).Value = group.Key.GSTPercent;
                    b2csSheet.Cell(b2csRowIdx, 4).Value = group.Sum(x => x.TaxableValue);
                    b2csSheet.Cell(b2csRowIdx, 5).Value = 0.00;
                    b2csRowIdx++;
                }
                b2csSheet.Columns().AdjustToContents();

                // --- 3. HSN Sheet ---
                var hsnSheet = workbook.Worksheets.Add("HSN");
                hsnSheet.Cell(1, 1).Value = "HSN";
                hsnSheet.Cell(1, 2).Value = "Description";
                hsnSheet.Cell(1, 3).Value = "UQC";
                hsnSheet.Cell(1, 4).Value = "Total Quantity";
                hsnSheet.Cell(1, 5).Value = "Total Value";
                hsnSheet.Cell(1, 6).Value = "Taxable Value";
                hsnSheet.Cell(1, 7).Value = "Integrated Tax Amount";
                hsnSheet.Cell(1, 8).Value = "Central Tax Amount";
                hsnSheet.Cell(1, 9).Value = "State/UT Tax Amount";
                hsnSheet.Cell(1, 10).Value = "Cess Amount";

                var hsnHeader = hsnSheet.Row(1);
                hsnHeader.Style.Font.Bold = true;
                hsnHeader.Style.Fill.BackgroundColor = XLColor.LightCyan;

                var hsnGroups = invoices.SelectMany(inv => inv.Items)
                    .GroupBy(item => {
                        productsDict.TryGetValue(item.ProductId, out var prod);
                        return prod?.HSNCode ?? "OTHERS";
                    });

                int hsnRowIdx = 2;
                foreach (var group in hsnGroups)
                {
                    string hsn = group.Key;
                    string description = "Goods";
                    if (group.Any() && productsDict.TryGetValue(group.First().ProductId, out var p))
                    {
                        description = p.Description ?? "Goods";
                    }

                    decimal qty = group.Sum(x => x.Qty);
                    decimal taxable = group.Sum(x => x.Total - x.TaxAmount);
                    decimal totalTax = group.Sum(x => x.TaxAmount);
                    
                    // We assume intra-state / local tax breakdown by default for HSN sheets if not specified
                    decimal cgst = group.Sum(x => x.TaxAmount / 2);
                    decimal sgst = group.Sum(x => x.TaxAmount / 2);
                    decimal igst = 0;

                    hsnSheet.Cell(hsnRowIdx, 1).Value = hsn;
                    hsnSheet.Cell(hsnRowIdx, 2).Value = description;
                    hsnSheet.Cell(hsnRowIdx, 3).Value = "PCS"; // Default UQC
                    hsnSheet.Cell(hsnRowIdx, 4).Value = qty;
                    hsnSheet.Cell(hsnRowIdx, 5).Value = taxable + totalTax;
                    hsnSheet.Cell(hsnRowIdx, 6).Value = taxable;
                    hsnSheet.Cell(hsnRowIdx, 7).Value = igst;
                    hsnSheet.Cell(hsnRowIdx, 8).Value = cgst;
                    hsnSheet.Cell(hsnRowIdx, 9).Value = sgst;
                    hsnSheet.Cell(hsnRowIdx, 10).Value = 0.00;
                    hsnRowIdx++;
                }
                hsnSheet.Columns().AdjustToContents();

                // --- 4. DOCS Sheet ---
                var docsSheet = workbook.Worksheets.Add("Documents");
                docsSheet.Cell(1, 1).Value = "Nature of Document";
                docsSheet.Cell(1, 2).Value = "Sr. No. From";
                docsSheet.Cell(1, 3).Value = "Sr. No. To";
                docsSheet.Cell(1, 4).Value = "Total Number";
                docsSheet.Cell(1, 5).Value = "Cancelled Number";
                docsSheet.Cell(1, 6).Value = "Net Number";

                var docsHeader = docsSheet.Row(1);
                docsHeader.Style.Font.Bold = true;
                docsHeader.Style.Fill.BackgroundColor = XLColor.LightCyan;

                if (invoices.Any())
                {
                    var sortedInvoices = invoices.OrderBy(i => i.InvoiceNo).ToList();
                    string firstNo = sortedInvoices.First().InvoiceNo;
                    string lastNo = sortedInvoices.Last().InvoiceNo;
                    int totalInvoices = sortedInvoices.Count;
                    
                    // We can check the database for cancelled invoices in the range
                    int cancelledCount = await _context.SalesInvoices
                        .CountAsync(i => i.CompanyId == companyId && i.InvoiceDate >= startDate && i.InvoiceDate <= endDate && i.Status == "Cancelled");

                    docsSheet.Cell(2, 1).Value = "Invoices for outward supply";
                    docsSheet.Cell(2, 2).Value = firstNo;
                    docsSheet.Cell(2, 3).Value = lastNo;
                    docsSheet.Cell(2, 4).Value = totalInvoices + cancelledCount;
                    docsSheet.Cell(2, 5).Value = cancelledCount;
                    docsSheet.Cell(2, 6).Value = totalInvoices;
                }
                docsSheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public async Task<Gstr3bSummaryDto> GetGstr3bSummaryAsync(DateTime startDate, DateTime endDate, Guid companyId)
        {
            var adjustedEndDate = endDate.Date.AddDays(1).AddTicks(-1);
            var summary = new Gstr3bSummaryDto();

            // 1. Calculate Outward Taxable Supplies (Sales)
            var sales = await _context.SalesInvoices
                .Where(i => i.CompanyId == companyId && i.InvoiceDate >= startDate.Date && i.InvoiceDate <= adjustedEndDate && i.Status != "Cancelled")
                .Select(i => new { i.SubTotal, i.TotalTax, i.IgstAmount, i.CgstAmount, i.SgstAmount })
                .ToListAsync();

            summary.OutwardSupplies.TaxableValue = sales.Sum(s => s.SubTotal);
            summary.OutwardSupplies.IntegratedTax = sales.Sum(s => s.IgstAmount ?? 0M);
            summary.OutwardSupplies.CentralTax = sales.Sum(s => s.CgstAmount ?? 0M);
            summary.OutwardSupplies.StateTax = sales.Sum(s => s.SgstAmount ?? 0M);
            summary.OutwardSupplies.Cess = 0M;

            // 2. Calculate Inward Taxable Supplies / Eligible ITC (Purchases from completed PurchaseOrders/GRNs)
            var purchases = await _context.PurchaseOrders
                .Where(p => p.CompanyId == companyId && p.PoDate >= startDate.Date && p.PoDate <= adjustedEndDate && (p.Status == "Received" || p.Status == "Completed" || p.Status == "ShortClosed"))
                .Select(p => new { p.SubTotal, p.TotalTax, p.IgstAmount, p.CgstAmount, p.SgstAmount })
                .ToListAsync();

            summary.InputTaxCredit.TaxableValue = purchases.Sum(p => p.SubTotal);
            summary.InputTaxCredit.IntegratedTax = purchases.Sum(p => p.IgstAmount ?? 0M);
            summary.InputTaxCredit.CentralTax = purchases.Sum(p => p.CgstAmount ?? 0M);
            summary.InputTaxCredit.StateTax = purchases.Sum(p => p.SgstAmount ?? 0M);
            summary.InputTaxCredit.Cess = 0M;

            // 3. Calculate Net Tax Payable
            summary.NetPayable.IntegratedTax = Math.Max(0M, summary.OutwardSupplies.IntegratedTax - summary.InputTaxCredit.IntegratedTax);
            summary.NetPayable.CentralTax = Math.Max(0M, summary.OutwardSupplies.CentralTax - summary.InputTaxCredit.CentralTax);
            summary.NetPayable.StateTax = Math.Max(0M, summary.OutwardSupplies.StateTax - summary.InputTaxCredit.StateTax);

            return summary;
        }

        public async Task<Gstr2bReconResultDto> ReconcileGstr2bAsync(Stream fileStream, string fileName, DateTime startDate, DateTime endDate, Guid companyId)
        {
            var result = new Gstr2bReconResultDto();
            var portalInvoices = new List<Gstr2bUploadRequestDto>();

            // --- 1. Parse Uploaded File ---
            if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                portalInvoices = ParseGstr2bJson(fileStream);
            }
            else if (fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                portalInvoices = ParseGstr2bExcel(fileStream);
            }
            else
            {
                throw new Exception("Unsupported file format. Please upload a GSTR-2B JSON or Excel file.");
            }

            // --- 2. Query Local ERP Purchase Records ---
            var erpPurchases = await _context.PurchaseOrders
                .Where(p => p.CompanyId == companyId && p.PoDate >= startDate.AddDays(-15) && p.PoDate <= endDate.AddDays(15))
                .ToListAsync();

            // Fetch supplier names & phones from microservice for fallback/notifications
            var supplierIds = erpPurchases.Select(p => p.SupplierId).Distinct().ToList();
            var suppliersList = supplierIds.Any() 
                ? await _supplierClient.GetSuppliersByIdsAsync(supplierIds)
                : new List<SupplierSelectDto>();
            var suppliersDict = suppliersList.ToDictionary(s => s.Id, s => s);

            // Match logs & tracking
            var matchedErpIds = new HashSet<Guid>();
            var matchedPortalIndices = new HashSet<int>();

            // Clean string helper for fuzzy invoice matching (removes slashes, dashes, spaces, and leading zeros)
            string CleanInvNo(string no)
            {
                if (string.IsNullOrEmpty(no)) return string.Empty;
                var clean = Regex.Replace(no.ToUpper(), @"[^A-Z0-9]", "");
                return clean.TrimStart('0');
            }

            // --- 3. Run Reconciliation Matching Engine ---

            // Step A: Exact and Fuzzy Matching loop (Match portal rows against ERP rows)
            for (int i = 0; i < portalInvoices.Count; i++)
            {
                var portalInv = portalInvoices[i];
                var cleanPortalNo = CleanInvNo(portalInv.InvoiceNo);

                // Find potential matches in ERP
                var erpMatch = erpPurchases.FirstOrDefault(erp => 
                    !matchedErpIds.Contains(erp.Id) &&
                    (CleanInvNo(erp.SupplierInvoiceNo ?? erp.PoNumber) == cleanPortalNo || CleanInvNo(erp.PoNumber) == cleanPortalNo) &&
                    (string.IsNullOrEmpty(portalInv.SupplierGstIn) || string.IsNullOrEmpty(erp.SupplierGstIn) || erp.SupplierGstIn.Trim().Equals(portalInv.SupplierGstIn.Trim(), StringComparison.OrdinalIgnoreCase))
                );

                if (erpMatch != null)
                {
                    matchedPortalIndices.Add(i);
                    matchedErpIds.Add(erpMatch.Id);

                    // Check for tax mismatches (tolerance: ₹2.00)
                    var taxDiff = Math.Abs(portalInv.TaxableValue - erpMatch.SubTotal) + 
                                  Math.Abs(portalInv.Cgst - (erpMatch.CgstAmount ?? 0)) +
                                  Math.Abs(portalInv.Sgst - (erpMatch.SgstAmount ?? 0)) +
                                  Math.Abs(portalInv.Igst - (erpMatch.IgstAmount ?? 0));

                    suppliersDict.TryGetValue(erpMatch.SupplierId, out var supplierInfo);

                    var row = new Gstr2bReconRowDto
                    {
                        Status = taxDiff <= 2.00M ? "Matched" : "Mismatched",
                        Reason = taxDiff <= 2.00M ? "Invoice fully matched" : $"Value mismatch of ₹{taxDiff:F2}",
                        
                        PortalSupplierGstIn = portalInv.SupplierGstIn,
                        PortalSupplierName = portalInv.SupplierName,
                        PortalInvoiceNo = portalInv.InvoiceNo,
                        PortalInvoiceDate = portalInv.InvoiceDate,
                        PortalTaxableValue = portalInv.TaxableValue,
                        PortalCgst = portalInv.Cgst,
                        PortalSgst = portalInv.Sgst,
                        PortalIgst = portalInv.Igst,
                        PortalGrandTotal = portalInv.GrandTotal,

                        ErpPurchaseOrderId = erpMatch.Id,
                        ErpSupplierGstIn = erpMatch.SupplierGstIn ?? supplierInfo?.GstIn,
                        ErpSupplierName = erpMatch.SupplierName ?? supplierInfo?.Name,
                        ErpInvoiceNo = erpMatch.SupplierInvoiceNo ?? erpMatch.PoNumber,
                        ErpInvoiceDate = erpMatch.SupplierInvoiceDate ?? erpMatch.PoDate,
                        ErpTaxableValue = erpMatch.SubTotal,
                        ErpCgst = erpMatch.CgstAmount ?? 0,
                        ErpSgst = erpMatch.SgstAmount ?? 0,
                        ErpIgst = erpMatch.IgstAmount ?? 0,
                        ErpGrandTotal = erpMatch.GrandTotal,

                        SupplierPhone = supplierInfo?.Phone
                    };

                    result.Rows.Add(row);
                }
            }

            // Step B: Invoices Missing in ERP (Present in Portal but not in ERP)
            for (int i = 0; i < portalInvoices.Count; i++)
            {
                if (matchedPortalIndices.Contains(i)) continue;

                var portalInv = portalInvoices[i];
                result.Rows.Add(new Gstr2bReconRowDto
                {
                    Status = "MissingInERP",
                    Reason = "Invoice uploaded by supplier but not recorded in ERP",
                    
                    PortalSupplierGstIn = portalInv.SupplierGstIn,
                    PortalSupplierName = portalInv.SupplierName,
                    PortalInvoiceNo = portalInv.InvoiceNo,
                    PortalInvoiceDate = portalInv.InvoiceDate,
                    PortalTaxableValue = portalInv.TaxableValue,
                    PortalCgst = portalInv.Cgst,
                    PortalSgst = portalInv.Sgst,
                    PortalIgst = portalInv.Igst,
                    PortalGrandTotal = portalInv.GrandTotal
                });
            }

            // Step C: Invoices Missing in Portal (Recorded in ERP but not in Portal)
            foreach (var erp in erpPurchases)
            {
                if (matchedErpIds.Contains(erp.Id)) continue;

                suppliersDict.TryGetValue(erp.SupplierId, out var supplierInfo);

                result.Rows.Add(new Gstr2bReconRowDto
                {
                    Status = "MissingInPortal",
                    Reason = "Recorded in ERP but not uploaded by supplier on portal",

                    ErpPurchaseOrderId = erp.Id,
                    ErpSupplierGstIn = erp.SupplierGstIn ?? supplierInfo?.GstIn,
                    ErpSupplierName = erp.SupplierName ?? supplierInfo?.Name,
                    ErpInvoiceNo = erp.SupplierInvoiceNo ?? erp.PoNumber,
                    ErpInvoiceDate = erp.SupplierInvoiceDate ?? erp.PoDate,
                    ErpTaxableValue = erp.SubTotal,
                    ErpCgst = erp.CgstAmount ?? 0,
                    ErpSgst = erp.SgstAmount ?? 0,
                    ErpIgst = erp.IgstAmount ?? 0,
                    ErpGrandTotal = erp.GrandTotal,
                    
                    SupplierPhone = supplierInfo?.Phone
                });
            }

            // --- 4. Compile Statistics ---
            result.Stats.TotalPortalCount = portalInvoices.Count;
            result.Stats.TotalErpCount = erpPurchases.Count;
            
            result.Stats.MatchedCount = result.Rows.Count(r => r.Status == "Matched");
            result.Stats.MismatchedCount = result.Rows.Count(r => r.Status == "Mismatched");
            result.Stats.MissingInPortalCount = result.Rows.Count(r => r.Status == "MissingInPortal");
            result.Stats.MissingInErpCount = result.Rows.Count(r => r.Status == "MissingInERP");

            result.Stats.TotalPortalTaxable = portalInvoices.Sum(p => p.TaxableValue);
            result.Stats.TotalPortalTax = portalInvoices.Sum(p => p.Cgst + p.Sgst + p.Igst);
            result.Stats.TotalErpTaxable = erpPurchases.Sum(e => e.SubTotal);
            result.Stats.TotalErpTax = erpPurchases.Sum(e => (e.CgstAmount ?? 0) + (e.SgstAmount ?? 0) + (e.IgstAmount ?? 0));

            return result;
        }

        private List<Gstr2bUploadRequestDto> ParseGstr2bJson(Stream fileStream)
        {
            var invoices = new List<Gstr2bUploadRequestDto>();
            using (var reader = new StreamReader(fileStream))
            {
                string jsonString = reader.ReadToEnd();
                using (var document = JsonDocument.Parse(jsonString))
                {
                    var root = document.RootElement;
                    
                    // Support standard Government Portal GSTR-2B JSON schema
                    if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("b2b", out var b2bArray))
                    {
                        foreach (var supplierElement in b2bArray.EnumerateArray())
                        {
                            string supplierGst = supplierElement.GetProperty("ctin").GetString() ?? string.Empty;
                            string supplierName = supplierElement.TryGetProperty("trdNm", out var trdNm) ? trdNm.GetString() ?? "" : "Supplier";

                            if (supplierElement.TryGetProperty("inv", out var invArray))
                            {
                                foreach (var inv in invArray.EnumerateArray())
                                {
                                    string invNo = inv.GetProperty("inum").GetString() ?? string.Empty;
                                    string invDateStr = inv.GetProperty("idt").GetString() ?? string.Empty;
                                    
                                    // Handle dates like "24-06-2026" or "2026-06-24"
                                    DateTime.TryParse(invDateStr, out var invDate);

                                    decimal totalVal = inv.TryGetProperty("val", out var val) ? val.GetDecimal() : 0;
                                    
                                    decimal taxable = 0, cgst = 0, sgst = 0, igst = 0;
                                    if (inv.TryGetProperty("itms", out var items))
                                    {
                                        foreach (var item in items.EnumerateArray())
                                        {
                                            if (item.TryGetProperty("itm_det", out var det))
                                            {
                                                taxable += det.TryGetProperty("txval", out var txval) ? txval.GetDecimal() : 0;
                                                cgst += det.TryGetProperty("camt", out var camt) ? camt.GetDecimal() : 0;
                                                sgst += det.TryGetProperty("samt", out var samt) ? samt.GetDecimal() : 0;
                                                igst += det.TryGetProperty("iamt", out var iamt) ? iamt.GetDecimal() : 0;
                                            }
                                        }
                                    }

                                    invoices.Add(new Gstr2bUploadRequestDto
                                    {
                                        SupplierGstIn = supplierGst,
                                        SupplierName = supplierName,
                                        InvoiceNo = invNo,
                                        InvoiceDate = invDate,
                                        TaxableValue = taxable,
                                        Cgst = cgst,
                                        Sgst = sgst,
                                        Igst = igst,
                                        GrandTotal = totalVal > 0 ? totalVal : (taxable + cgst + sgst + igst)
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        // Fallback: Check if it's a direct JSON array of invoices
                        try
                        {
                            var list = JsonSerializer.Deserialize<List<Gstr2bUploadRequestDto>>(jsonString, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            if (list != null) invoices.AddRange(list);
                        }
                        catch { }
                    }
                }
            }
            return invoices;
        }

        private List<Gstr2bUploadRequestDto> ParseGstr2bExcel(Stream fileStream)
        {
            var invoices = new List<Gstr2bUploadRequestDto>();
            using (var workbook = new XLWorkbook(fileStream))
            {
                // Look for B2B sheet (it might be named "B2B", "b2b", "B2B Invoices", etc.)
                var b2bSheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("B2B", StringComparison.OrdinalIgnoreCase)) 
                               ?? workbook.Worksheets.FirstOrDefault();

                if (b2bSheet == null) return invoices;

                // Find header row (usually contains GSTIN of supplier or Invoice number)
                int headerRowIdx = 1;
                for (int r = 1; r <= 10; r++)
                {
                    for (int c = 1; c <= 15; c++)
                    {
                        var cellVal = b2bSheet.Cell(r, c).GetString();
                        if (cellVal.Contains("GSTIN", StringComparison.OrdinalIgnoreCase) || cellVal.Contains("Invoice", StringComparison.OrdinalIgnoreCase))
                        {
                            headerRowIdx = r;
                            break;
                        }
                    }
                }

                // Map header column indices
                int gstinCol = -1, nameCol = -1, invNoCol = -1, dateCol = -1, taxableCol = -1, cgstCol = -1, sgstCol = -1, igstCol = -1, totalCol = -1;
                var headerRow = b2bSheet.Row(headerRowIdx);
                for (int c = 1; c <= 30; c++)
                {
                    string colText = headerRow.Cell(c).GetString().Trim().ToLower();
                    if (string.IsNullOrEmpty(colText)) continue;

                    if (colText.Contains("gstin") || colText.Contains("supplier gstin")) gstinCol = c;
                    else if (colText.Contains("name") || colText.Contains("supplier name") || colText.Contains("trade name")) nameCol = c;
                    else if (colText.Contains("invoice number") || colText.Contains("invoice no") || colText.Contains("inv no") || colText.Equals("inum")) invNoCol = c;
                    else if (colText.Contains("date") || colText.Contains("dt")) dateCol = c;
                    else if (colText.Contains("taxable value") || colText.Contains("txval")) taxableCol = c;
                    else if (colText.Contains("central tax") || colText.Contains("cgst") || colText.Contains("camt")) cgstCol = c;
                    else if (colText.Contains("state tax") || colText.Contains("sgst") || colText.Contains("samt")) sgstCol = c;
                    else if (colText.Contains("integrated tax") || colText.Contains("igst") || colText.Contains("iamt")) igstCol = c;
                    else if (colText.Contains("invoice value") || colText.Contains("grand total") || colText.Contains("total value")) totalCol = c;
                }

                // Parse data rows
                int lastRow = b2bSheet.LastRowUsed()?.RowNumber() ?? 0;
                for (int r = headerRowIdx + 1; r <= lastRow; r++)
                {
                    var row = b2bSheet.Row(r);
                    string gstin = gstinCol > 0 ? row.Cell(gstinCol).GetString().Trim() : string.Empty;
                    
                    if (string.IsNullOrWhiteSpace(gstin)) continue; // End of records or empty row

                    string name = nameCol > 0 ? row.Cell(nameCol).GetString().Trim() : "Supplier";
                    string invNo = invNoCol > 0 ? row.Cell(invNoCol).GetString().Trim() : string.Empty;
                    
                    DateTime invDate = DateTime.MinValue;
                    if (dateCol > 0)
                    {
                        var cell = row.Cell(dateCol);
                        if (cell.DataType == XLDataType.DateTime) invDate = cell.GetDateTime();
                        else DateTime.TryParse(cell.GetString(), out invDate);
                    }

                    decimal taxable = taxableCol > 0 ? GetDecimalValue(row.Cell(taxableCol)) : 0;
                    decimal cgst = cgstCol > 0 ? GetDecimalValue(row.Cell(cgstCol)) : 0;
                    decimal sgst = sgstCol > 0 ? GetDecimalValue(row.Cell(sgstCol)) : 0;
                    decimal igst = igstCol > 0 ? GetDecimalValue(row.Cell(igstCol)) : 0;
                    decimal totalVal = totalCol > 0 ? GetDecimalValue(row.Cell(totalCol)) : (taxable + cgst + sgst + igst);

                    invoices.Add(new Gstr2bUploadRequestDto
                    {
                        SupplierGstIn = gstin,
                        SupplierName = name,
                        InvoiceNo = invNo,
                        InvoiceDate = invDate,
                        TaxableValue = taxable,
                        Cgst = cgst,
                        Sgst = sgst,
                        Igst = igst,
                        GrandTotal = totalVal
                    });
                }
            }
            return invoices;
        }

        private decimal GetDecimalValue(IXLCell cell)
        {
            var val = cell.Value;
            if (val.IsNumber)
            {
                return (decimal)val.GetNumber();
            }

            var str = val.ToString()?.Trim();
            if (decimal.TryParse(str, out decimal d))
            {
                return d;
            }

            return 0;
        }

        private string GetPlaceOfSupply(string? gstin, string? placeOfSupply)
        {
            if (!string.IsNullOrEmpty(placeOfSupply) && placeOfSupply.Contains("-") && placeOfSupply != "State Code" && placeOfSupply != "Local State")
            {
                return placeOfSupply;
            }

            if (!string.IsNullOrEmpty(placeOfSupply) && placeOfSupply.Length > 2 && placeOfSupply != "State Code" && placeOfSupply != "Local State")
            {
                return placeOfSupply;
            }

            // Try to extract from GSTIN
            if (!string.IsNullOrEmpty(gstin) && gstin.Length >= 2 && char.IsDigit(gstin[0]) && char.IsDigit(gstin[1]))
            {
                string code = gstin.Substring(0, 2);
                switch (code)
                {
                    case "01": return "01-Jammu & Kashmir";
                    case "02": return "02-Himachal Pradesh";
                    case "03": return "03-Punjab";
                    case "04": return "04-Chandigarh";
                    case "05": return "05-Uttarakhand";
                    case "06": return "06-Haryana";
                    case "07": return "07-Delhi";
                    case "08": return "08-Rajasthan";
                    case "09": return "09-Uttar Pradesh";
                    case "10": return "10-Bihar";
                    case "11": return "11-Sikkim";
                    case "12": return "12-Arunachal Pradesh";
                    case "13": return "13-Nagaland";
                    case "14": return "14-Manipur";
                    case "15": return "15-Mizoram";
                    case "16": return "16-Tripura";
                    case "17": return "17-Meghalaya";
                    case "18": return "18-Assam";
                    case "19": return "19-West Bengal";
                    case "20": return "20-Jharkhand";
                    case "21": return "21-Odisha";
                    case "22": return "22-Chhattisgarh";
                    case "23": return "23-Madhya Pradesh";
                    case "24": return "24-Gujarat";
                    case "26": return "26-Dadra & Nagar Haveli and Daman & Diu";
                    case "27": return "27-Maharashtra";
                    case "29": return "29-Karnataka";
                    case "30": return "30-Goa";
                    case "31": return "31-Lakshadweep";
                    case "32": return "32-Kerala";
                    case "33": return "33-Tamil Nadu";
                    case "34": return "34-Puducherry";
                    case "35": return "35-Andaman & Nicobar Islands";
                    case "36": return "36-Telangana";
                    case "37": return "37-Andhra Pradesh";
                    case "38": return "38-Ladakh";
                }
            }

            return "10-Bihar"; // Default local state for Anand Furniture
        }
    }
}

using System;
using System.Collections.Generic;

namespace Inventory.Application.Gst.DTOs
{
    public class Gstr2bReconResultDto
    {
        public Gstr2bReconStatsDto Stats { get; set; } = new();
        public List<Gstr2bReconRowDto> Rows { get; set; } = new();
    }

    public class Gstr2bReconStatsDto
    {
        public int TotalPortalCount { get; set; }
        public int TotalErpCount { get; set; }
        
        public int MatchedCount { get; set; }
        public int MismatchedCount { get; set; }
        public int MissingInPortalCount { get; set; }
        public int MissingInErpCount { get; set; }

        public decimal TotalPortalTaxable { get; set; }
        public decimal TotalPortalTax { get; set; }
        public decimal TotalErpTaxable { get; set; }
        public decimal TotalErpTax { get; set; }
    }

    public class Gstr2bReconRowDto
    {
        // Status: "Matched", "Mismatched", "MissingInPortal", "MissingInERP"
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;

        // Portal Invoice Details
        public string? PortalSupplierGstIn { get; set; }
        public string? PortalSupplierName { get; set; }
        public string? PortalInvoiceNo { get; set; }
        public DateTime? PortalInvoiceDate { get; set; }
        public decimal PortalTaxableValue { get; set; }
        public decimal PortalCgst { get; set; }
        public decimal PortalSgst { get; set; }
        public decimal PortalIgst { get; set; }
        public decimal PortalTotalTax => PortalCgst + PortalSgst + PortalIgst;
        public decimal PortalGrandTotal { get; set; }

        // ERP Invoice Details (from PurchaseOrder/GRN)
        public Guid? ErpPurchaseOrderId { get; set; }
        public string? ErpSupplierGstIn { get; set; }
        public string? ErpSupplierName { get; set; }
        public string? ErpInvoiceNo { get; set; }
        public DateTime? ErpInvoiceDate { get; set; }
        public decimal ErpTaxableValue { get; set; }
        public decimal ErpCgst { get; set; }
        public decimal ErpSgst { get; set; }
        public decimal ErpIgst { get; set; }
        public decimal ErpTotalTax => ErpCgst + ErpSgst + ErpIgst;
        public decimal ErpGrandTotal { get; set; }
        
        // WhatsApp nudge link helper
        public string? SupplierPhone { get; set; }
    }

    public class Gstr2bUploadRequestDto
    {
        // The base64 or raw file stream is processed in the controller, but this is the schema of parsed portal rows.
        public string SupplierGstIn { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal TaxableValue { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Igst { get; set; }
        public decimal GrandTotal { get; set; }
    }
}

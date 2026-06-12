using System;

namespace Inventory.Application.SalesInvoices.DTOs
{
    public class UnifiedSaleDto
    {
        public Guid Id { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public Guid? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }
        public decimal TotalTax { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty; // "QuickSale" or "TaxInvoice"
        public string CreatedBy { get; set; } = string.Empty;
        public Guid? DeliveryChallanId { get; set; }
        public string? ChallanNo { get; set; }
        public bool IsQuick { get; set; }
        public decimal TotalQty { get; set; }
        public string? GatePassNo { get; set; }
        public string? DoctorName { get; set; }
        public string? DoctorRegNo { get; set; }
    }
}

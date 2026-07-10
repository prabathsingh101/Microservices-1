using System;
using System.Collections.Generic;

namespace Inventory.Application.SalesInvoices.DTOs
{
    public class CreateSalesInvoiceDto
    {
        public Guid Id { get; set; }
        public string? InvoiceNo { get; set; } // Auto-generated if null
        public DateTime InvoiceDate { get; set; }
        public Guid? CustomerId { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal GrandTotal { get; set; }
        public string? TaxType { get; set; }
        public decimal? IgstAmount { get; set; }
        public decimal? CgstAmount { get; set; }
        public decimal? SgstAmount { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string Status { get; set; } = "Confirmed";
        public bool IsQuick { get; set; }
        public string? GuestName { get; set; }
        public string? GuestPhone { get; set; }
        public Guid? DeliveryChallanId { get; set; }
        public List<Guid>? DeliveryChallanIds { get; set; }
        public string? DoctorName { get; set; }
        public string? DoctorRegNo { get; set; }
        public string? CustomerGstIn { get; set; }
        public string? CustomerName { get; set; }
        public string? PlaceOfSupply { get; set; }
        
        public Guid? CompanyId { get; set; }
        public string? BranchId { get; set; }
        public string? CreatedBy { get; set; }

        public List<SalesInvoiceItemDto> Items { get; set; } = new();
    }

    public class SalesInvoiceItemDto
    {
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public decimal MRP { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal GstPercent { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
        
        public Guid? WarehouseId { get; set; }
        public Guid? RackId { get; set; }
        public string? BatchNumber { get; set; }
        public string? ReferenceNumber { get; set; }
        
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}

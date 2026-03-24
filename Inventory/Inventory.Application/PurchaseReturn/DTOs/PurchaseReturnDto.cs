using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PurchaseReturn.DTOs
{
    public class PurchaseReturnDto
    {
        public int SupplierId { get; set; } // Changed to int [cite: 2026-02-03]
        public DateTime ReturnDate { get; set; }
        public string Remarks { get; set; }
        public bool IsQuick { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public List<PurchaseReturnItemDto> Items { get; set; }
    }

    public class PurchaseReturnItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string GrnRef { get; set; }
        public decimal ReturnQty { get; set; }
        public decimal Rate { get; set; }
        public decimal GstPercent {  get; set; }
        public decimal DiscountPercent {  get; set; }

        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public DateTime? MfgDate { get; set; }
        public DateTime? ExpDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PurchaseReturn.DTOs
{
    // PurchaseReturnDetailDto.cs
    public class PurchaseReturnDetailDto
    {
        public Guid Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public long SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string Status { get; set; }
        public bool IsQuick { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal SubTotal { get; set; }
        public decimal GstAmount { get; set; }
        public decimal TaxAmount { get; set; }

        public List<PurchaseReturnItemDto> Items { get; set; } = new();
    }

 
}

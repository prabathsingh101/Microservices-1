using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PurchaseReturn.DTOs
{
    public class PurchaseReturnDto
    {
        public Guid SupplierId { get; set; } // Changed to Guid
        public DateTime ReturnDate { get; set; }
        public string Remarks { get; set; }
        public bool IsQuick { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public Guid? CompanyId { get; set; }
        public string? BranchId { get; set; }
        public List<PurchaseReturnItemDto> Items { get; set; }
    }

    public class PurchaseReturnItemDto
    {
        public Guid? CompanyId { get; set; }
        public string? BranchId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string GrnRef { get; set; }
        public decimal ReturnQty { get; set; }
        public decimal Rate { get; set; }
        public decimal GstPercent {  get; set; }
        public decimal DiscountPercent {  get; set; }
        public Guid? WarehouseId { get; set; }
        public Guid? RackId { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public DateTime? MfgDate { get; set; }
        public DateTime? ExpDate { get; set; }
        public bool IsExpiryRequired { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public Guid? ProductVariantId { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
    }
}

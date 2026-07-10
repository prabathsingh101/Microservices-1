using System;

namespace Inventory.Application.PurchaseReturn.DTOs
{
    public class ReceivedStockDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string GrnRef { get; set; } // GRN Number
        public decimal AvailableQty { get; set; } // AcceptedQty or Current Stock
        public decimal Rate { get; set; }
        public decimal GstPercent { get; set; }
        public decimal DiscountPercent { get; set; }
        public Guid? WarehouseId { get; set; }
        public Guid? RackId { get; set; }
        public DateTime ReceivedDate { get; set; }
        public decimal CurrentStock { get; set; }
        public string? WarehouseName { get; set; }
        public string? RackName { get; set; }
        public DateTime? MfgDate { get; set; }
        public DateTime? ExpDate { get; set; }
        public string? BranchId { get; set; }
        public bool IsReturnable { get; set; } = true;
        public double RemainingHours { get; set; }
        public string? PoNumber { get; set; }
        public string? Brand { get; set; }
        public string? Sku { get; set; }
        public Guid? ProductVariantId { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
    }
}

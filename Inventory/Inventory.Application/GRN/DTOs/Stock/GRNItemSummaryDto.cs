using System;

namespace Inventory.Application.GRN.DTOs.Stock
{
    public class GRNItemSummaryDto
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal OrderedQty { get; set; }
        public decimal PendingQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal AcceptedQty { get; set; }
        public decimal RejectedQty { get; set; }
        public decimal ActualRejectedQty { get; set; }
        public decimal ExpiredQty { get; set; }
        public decimal UnitRate { get; set; }
        public string? RackName { get; set; }
        public bool IsExpired { get; set; }
        public decimal ReturnedQty { get; set; }
        public string? WarehouseName { get; set; }
        public DateTime? MfgDate { get; set; }
        public DateTime? ExpDate { get; set; }
        public Guid? ProductVariantId { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
    }
}

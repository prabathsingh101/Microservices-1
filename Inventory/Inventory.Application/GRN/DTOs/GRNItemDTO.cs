using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.GRN.DTOs
{
    public class GRNItemDTO
    {
        public Guid ProductId { get; set; } // Based on your DB schema
        public decimal OrderedQty { get; set; }
        public decimal PendingQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal RejectedQty { get; set; }
        public decimal AcceptedQty { get; set; }
        public decimal UnitRate { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal GstPercent { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public Guid? WarehouseId { get; set; }
        public Guid? RackId { get; set; }
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}

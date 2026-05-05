using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.GRN.DTOs
{
    public class POItemForGRNDTO
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal OrderedQty { get; set; }
        public decimal PendingQty { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal GstPercent { get; set; }
        public decimal UnitRate { get; set; }

        public decimal ReceivedQty { get; set; }
        public decimal RejectedQty { get; set; }
        public decimal AcceptedQty { get; set; }
        public decimal TaxAmount { get; set; }
        public string? PONumber { get; set; }
        public Guid POId { get; set; }
        public Guid SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public bool IsReplacement { get; set; }
        public Guid? WarehouseId { get; set; }
        public Guid? RackId { get; set; }

        // Dates from the PO items / saved GRN details
        public DateTime? MfgDate { get; set; }
        public DateTime? ExpDate { get; set; }
        public string? BatchNumber { get; set; }
    }
}

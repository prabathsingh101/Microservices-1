using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PurchaseOrders.DTOs
{
    public class UpdatePurchaseOrderItemDto
    {
        public int Id { get; set; } // Agar 0 hai toh naya item add hoga
        public Guid ProductId { get; set; } //
        public decimal Qty { get; set; }
        public string? Unit { get; set; }
        public decimal Rate { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal GstPercent { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}

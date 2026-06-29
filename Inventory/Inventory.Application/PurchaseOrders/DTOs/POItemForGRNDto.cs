using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PurchaseOrders.DTOs
{
    public class POItemForGRNDto
    {
        public Guid ProductId { get; set; } // int se Guid kar diya
        public string ProductName { get; set; }
        public decimal OrderedQty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal AlreadyReceivedQty { get; set; }
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsExpiryRequired { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal GstPercent { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
    }
}

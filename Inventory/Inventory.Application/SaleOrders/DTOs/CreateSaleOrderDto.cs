using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.SaleOrders.DTOs
{
    public class CreateSaleOrderDto
    {
        public int Id { get; set; }
        public string? SONumber { get; set; } // Add this for consistency
        public int CustomerId { get; set; }
        public DateTime SoDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string Remarks { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal GrandTotal { get; set; }
        public string Status { get; set; } = "Confirmed";
        public string CreatedBy { get; set; }
        public bool IsQuick { get; set; } = false;
        public List<SaleOrderItemDto> Items { get; set; }
    }

    public class SaleOrderItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Qty { get; set; }
        public string Unit { get; set; }
        public decimal Rate { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal GstPercent { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
        public string? RackName { get; set; }
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}

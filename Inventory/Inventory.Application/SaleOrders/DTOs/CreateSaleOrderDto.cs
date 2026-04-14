using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.SaleOrders.DTOs
{
    public class CreateSaleOrderDto
    {
        public Guid Id { get; set; }
        public string? SONumber { get; set; } // Add this for consistency
        public Guid CustomerId { get; set; }
        public DateTime SoDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string Remarks { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal GrandTotal { get; set; }
        public string? TaxType { get; set; } // local, interState
        public decimal? TdsPercent { get; set; }
        public decimal? TdsAmount { get; set; }
        public decimal? TcsPercent { get; set; }
        public decimal? TcsAmount { get; set; }
        public decimal? IgstAmount { get; set; }
        public decimal? CgstAmount { get; set; }
        public decimal? SgstAmount { get; set; }
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
        public decimal MRP { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal GstPercent { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
        public Guid? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public Guid? RackId { get; set; }
        public string? RackName { get; set; }
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}

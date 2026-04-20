using System;
using System.Collections.Generic;

namespace Inventory.Application.GRN.DTOs.Stock
{
    public class StockSummaryDto
    {
        public Guid ProductId { get; set; }
        public Guid? LastSupplierId { get; set; }
        public Guid? LastPurchaseOrderId { get; set; }
        public string? ProductName { get; set; }
        public decimal TotalReceived { get; set; }
        public string? Unit { get; set; }
        public decimal LastRate { get; set; }
        public bool IsLowStock => AvailableStock < 10;
        public int MinStockLevel { get; set; }
        public decimal AvailableStock { get; set; }
        public decimal TotalRejected { get; set; }
        public decimal TotalExpired { get; set; }
        public decimal TotalSold { get; set; }
        public bool IsAlreadyPurged { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public DateTime? PurgedDate { get; set; }

        public Guid? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public Guid? RackId { get; set; }
        public string? RackName { get; set; }

        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public string? Sku { get; set; }
        public decimal GstPercent { get; set; }
        public bool IsExpiryRequired { get; set; }

        public List<StockHistoryDto> History { get; set; } = new List<StockHistoryDto>();
    }

    public class StockHistoryDto
    {
        public Guid? ProductId { get; set; }
        public Guid? WarehouseId { get; set; }
        public Guid? RackId { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string? PONumber { get; set; }
        public string? GRNNumber { get; set; }
        public string? SupplierName { get; set; }
        public string? TransactionType { get; set; }
        public string? ProductName { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal RejectedQty { get; set; }
        public decimal ExpiredQty { get; set; }
        public string? WarehouseName { get; set; }
        public string? RackName { get; set; }
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsExpiryRequired { get; set; }
        public decimal AvailableQty { get; set; }
        public bool IsAlreadyPurged { get; set; }
    }
}

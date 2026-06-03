using System;
using System.Collections.Generic;

namespace Inventory.Application.DeliveryChallans.DTOs
{
    public class DeliveryChallanDto
    {
        public Guid Id { get; set; }
        public string? ChallanNo { get; set; }
        public DateTime? ChallanDate { get; set; }
        public Guid? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public decimal? SubTotal { get; set; }
        public decimal? TotalTax { get; set; }
        public decimal? GrandTotal { get; set; }
        public string? Remarks { get; set; }
        public string? Status { get; set; } = "Pending";
        public string? CancelReason { get; set; }
        
        // GTA details
        public decimal? GrossWeight { get; set; }
        public string? VehicleRegNo { get; set; }
        public string? Origin { get; set; }
        public string? Destination { get; set; }
        
        public Guid? CompanyId { get; set; }
        public string? BranchId { get; set; }
        public string? CreatedBy { get; set; }

        public List<DeliveryChallanItemDto> Items { get; set; } = new();
    }

    public class DeliveryChallanItemDto
    {
        public Guid Id { get; set; }
        public Guid? ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal? Qty { get; set; }
        public string? Unit { get; set; }
        public decimal? Rate { get; set; }
        public decimal? MRP { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? GstPercent { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? Total { get; set; }
        
        public Guid? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public Guid? RackId { get; set; }
        public string? RackName { get; set; }
        public string? BatchNumber { get; set; }
        
        public DateTime? MfgDate { get; set; }
        public DateTime? ExpDate { get; set; }
    }
}

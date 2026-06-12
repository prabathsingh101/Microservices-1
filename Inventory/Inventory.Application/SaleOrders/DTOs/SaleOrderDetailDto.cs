using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.SaleOrders.DTOs
{
    public class SaleOrderDetailDto
    {
        public Guid Id { get; set; }
        public string SoNumber { get; set; } = string.Empty;
        public DateTime SoDate { get; set; }
        public Guid? CustomerId { get; set; }
        public string CustomerName { get; set; } = "Loading..."; // Placeholder for Microservice data
        public string? GuestName { get; set; }
        public string? GuestPhone { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public string? TaxType { get; set; }
        public decimal? TdsPercent { get; set; }
        public decimal? TdsAmount { get; set; }
        public decimal? TcsPercent { get; set; }
        public decimal? TcsAmount { get; set; }
        public decimal? IgstAmount { get; set; }
        public decimal? CgstAmount { get; set; }
        public decimal? SgstAmount { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string? BranchId { get; set; }
        public Guid CompanyId { get; set; }
        public bool IsQuick { get; set; }
        public string? CancelReason { get; set; }
        public string? DoctorName { get; set; }
        public string? DoctorRegNo { get; set; }

        public List<SaleOrderItemDto> Items { get; set; } = new();
    }
}

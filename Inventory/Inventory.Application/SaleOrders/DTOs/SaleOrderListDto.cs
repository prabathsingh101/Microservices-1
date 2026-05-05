using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.SaleOrders.DTOs
{
    public class SaleOrderListDto
    {
        public Guid Id { get; set; }
        public string SoNumber { get; set; } = string.Empty;
        public DateTime SoDate { get; set; }
        public Guid CustomerId { get; set; } // Mapping ke liye zaroori hai
        public string CustomerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? GatePassNo { get; set; }
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
        public decimal TotalQty { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public bool IsReturnable { get; set; } = true;
        public List<SaleOrderItemDto> Items { get; set; } = new();
    }
}

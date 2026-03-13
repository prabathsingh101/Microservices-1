using Inventory.Application.GRN.DTOs.Stock;

namespace Inventory.Application.GRN.DTOs
{
    public class GRNListDto
    {
        public int Id { get; set; }
        public string GRNNo { get; set; }
        public string RefPO { get; set; }
        public string SupplierName { get; set; }
        public int SupplierId { get; set; }  // For payment navigation
        public DateTime ReceivedDate { get; set; }
        public string Status { get; set; } // Completed or Partial
        public string? GatePassNo { get; set; }
        public string PaymentStatus { get; set; } = "Unpaid"; // Paid, Partial, Unpaid
        public decimal TotalAmount { get; set; }  // GRN Total Amount
        public decimal PaidAmount { get; set; }   // Already paid
                                           // Yeh do fields expansion aur badge logic ke liye zaroori hain
        public decimal TotalRejected { get; set; }
        public decimal TotalActualRejected { get; set; }
        public decimal TotalExpired { get; set; }
        public List<GRNItemSummaryDto> Items { get; set; } = new List<GRNItemSummaryDto>();
    }
    // Paged Response [cite: 2026-01-22]
    public class GRNPagedResponseDto
    {
        public List<GRNListDto> Items { get; set; }
        public int TotalCount { get; set; }
    }
}

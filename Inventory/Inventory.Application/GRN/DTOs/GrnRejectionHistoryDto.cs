using System;

namespace Inventory.Application.GRN.DTOs
{
    public class GrnRejectionHistoryDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal RejectedQty { get; set; }
        public bool IsSettled { get; set; }
        public string Status { get; set; } = "Pending"; // Pending / Settled
        public string? Resolution { get; set; } // Replaced in GRN-xxxx / Returned / Pending
        public DateTime? ResolutionDate { get; set; }
        public string? ResolutionGrn { get; set; }
        public Guid? ResolutionGrnId { get; set; }
    }
}

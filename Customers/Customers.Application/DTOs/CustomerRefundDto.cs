using System;

namespace Customers.Application.DTOs
{
    public class CustomerRefundDto
    {
        public Guid? CustomerId { get; set; }
        public decimal Amount { get; set; }
        public DateTime RefundDate { get; set; }
        public string? RefundMode { get; set; } // Cash, UPI, Bank Transfer
        public string? ReferenceNumber { get; set; }
        public string? Remarks { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? BranchId { get; set; }
        public Guid? CompanyId { get; set; }
    }
}

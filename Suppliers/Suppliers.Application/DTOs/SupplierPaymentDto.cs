using System;

namespace Suppliers.Application.DTOs;

public class SupplierPaymentDto
{
    public Guid SupplierId { get; set; }
    public Guid CompanyId { get; set; }
    public string? BranchId { get; set; } // Added for multi-tenancy
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMode { get; set; } = "Cash";
    public string? ReferenceNumber { get; set; }
    public string? Remarks { get; set; }
    public string TransactionType { get; set; } = "Payment"; // "Payment" or "Refund"
    public string CreatedBy { get; set; } = "System";
}

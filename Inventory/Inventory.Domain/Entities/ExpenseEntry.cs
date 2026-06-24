using Inventory.Domain.Common;
using System;

namespace Inventory.Domain.Entities;

public class ExpenseEntry : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public virtual ExpenseCategory? Category { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string PaymentMode { get; set; } = null!; // Cash, Bank, UPI, etc.
    public string? ReferenceNo { get; set; }
    public string? Remarks { get; set; }
    public string? AttachmentPath { get; set; }

    // --- RCM (Reverse Charge Mechanism) Tracker ---
    public bool IsRcm { get; set; } = false;
    public decimal? RcmGstRate { get; set; }
    public decimal? RcmTaxableValue { get; set; }
    public decimal? RcmTaxAmount { get; set; }
    public decimal? RcmCgstAmount { get; set; }
    public decimal? RcmSgstAmount { get; set; }
    public decimal? RcmIgstAmount { get; set; }
    public bool RcmPaid { get; set; } = false;
    public DateTime? RcmPaidDate { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierGstin { get; set; }
}

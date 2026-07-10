using System;

namespace Shared.Contracts
{
    public interface SupplierPurchaseCreatedEvent
    {
        Guid SupplierId { get; }
        decimal Amount { get; }
        string ReferenceId { get; }
        string Description { get; }
        DateTime TransactionDate { get; }
        string CreatedBy { get; }
        Guid? CompanyId { get; }
        string? BranchId { get; }
    }

    public interface SupplierPurchaseReturnCreatedEvent
    {
        Guid SupplierId { get; }
        decimal Amount { get; }
        string ReferenceId { get; }
        string Description { get; }
        DateTime TransactionDate { get; }
        string CreatedBy { get; }
        Guid? CompanyId { get; }
        string? BranchId { get; }
    }

    public interface SupplierPaymentCreatedEvent
    {
        Guid SupplierId { get; }
        decimal Amount { get; }
        string ReferenceNumber { get; }
        string Remarks { get; }
        string PaymentMode { get; }
        DateTime PaymentDate { get; }
        string CreatedBy { get; }
        Guid? CompanyId { get; }
        string? BranchId { get; }
        string? TransactionType { get; }
    }

    public interface CustomerSaleCreatedEvent
    {
        Guid CustomerId { get; }
        decimal Amount { get; }
        string ReferenceId { get; }
        string Description { get; }
        DateTime TransactionDate { get; }
        string CreatedBy { get; }
        string? BranchId { get; }
        Guid? CompanyId { get; }
    }

    public interface CustomerReceiptCreatedEvent
    {
        Guid? CustomerId { get; }
        decimal Amount { get; }
        DateTime PaymentDate { get; }
        string PaymentMode { get; }
        string ReferenceNumber { get; }
        string Remarks { get; }
        string CreatedBy { get; }
        string? BranchId { get; }
        Guid? CompanyId { get; }
    }
}

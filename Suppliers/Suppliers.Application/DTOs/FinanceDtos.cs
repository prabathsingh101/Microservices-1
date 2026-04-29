using Suppliers.Domain.Entities;
using System;
using System.Collections.Generic;

namespace Suppliers.Application.DTOs
{
    public class SupplierLedgerResultDto
    {
        public string? SupplierName { get; set; }
        public List<SupplierLedger> Ledger { get; set; } = new();
    }

    public class SupplierLedgerRequestDto
    {
        public Guid SupplierId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchTerm { get; set; }
        public string? TypeFilter { get; set; }
        public string? ReferenceFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "TransactionDate";
        public string SortOrder { get; set; } = "desc";
    }

    public class SupplierLedgerPagedResultDto
    {
        public string? SupplierName { get; set; }
        public decimal currentBalance { get; set; }
        public PaginatedListDto<SupplierLedger> Ledger { get; set; } = new();
    }

    public class PendingDueDto
    {
        public Guid SupplierId { get; set; }
        public decimal PendingAmount { get; set; }
        public string? SupplierName { get; set; }
        public string? Status { get; set; }
        public DateTime DueDate { get; set; }
        public string? LastReferenceId { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class DateRangeDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? BranchId { get; set; }
        public string? CompanyId { get; set; }
    }

    public class SupplierPurchaseDto
    {
        public Guid SupplierId { get; set; }
        public Guid CompanyId { get; set; }
        public string? BranchId { get; set; } // Added for multi-tenancy
        public decimal Amount { get; set; }
        public string? ReferenceId { get; set; } // GRN Number
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? TransactionType { get; set; } // Added for Debit Notes
    }

    public class PaymentReportDto
    {
        public Guid Id { get; set; }
        public Guid SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PaymentMode { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class PaymentReportRequestDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Guid? SupplierId { get; set; }
        public string? SearchTerm { get; set; }
        public string? BranchId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "PaymentDate";
        public string SortOrder { get; set; } = "desc";
    }

    public class PaginatedListDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public class MonthlyTrendDto
    {
        public string? Month { get; set; }
        public decimal Amount { get; set; }
    }
}

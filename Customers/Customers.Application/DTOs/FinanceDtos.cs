using Customers.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Customers.Application.DTOs
{
    public class CustomerLedgerResultDto
    {
        public string? CustomerName { get; set; }
        public List<CustomerLedger> Ledger { get; set; } = new();
    }

    public class CustomerLedgerRequestDto
    {
        public Guid CustomerId { get; set; }
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

    public class CustomerLedgerPagedResultDto
    {
        public string? CustomerName { get; set; }
        public decimal CurrentBalance { get; set; }
        public PaginatedListDto<CustomerLedger> Ledger { get; set; } = new();
    }

    public class PaginatedListDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public class CustomerReceiptDto
    {
        public Guid CustomerId { get; set; }
        public decimal Amount { get; set; }

        [JsonPropertyName("paymentDate")]
        public DateTime ReceiptDate { get; set; }

        [JsonPropertyName("paymentMode")]
        public string? ReceiptMode { get; set; } // Cash, Bank, etc.
        public string? ReferenceNumber { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class OutstandingDto
    {
        public Guid CustomerId { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? CustomerName { get; set; }
        public string? Phone { get; set; }  // WhatsApp reminder ke liye
        public string? Status { get; set; }
        public DateTime DueDate { get; set; }
        public string? LastReferenceId { get; set; }
    }

    public class OutstandingRequestDto
    {
        public string? SearchTerm { get; set; }
        public string? CustomerNameFilter { get; set; }
        public string? StatusFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "CustomerName";
        public string SortOrder { get; set; } = "asc";
    }

    public class OutstandingPagedResultDto
    {
        public PaginatedListDto<OutstandingDto> Items { get; set; } = new();
        public decimal TotalOutstandingAmount { get; set; }
    }

    public class DateRangeDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class CustomerSaleDto
    {
        public Guid CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string? ReferenceId { get; set; } // Invoice/Sale Order Number
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? CreatedBy { get; set; }
        public Guid? CompanyId { get; set; }
    }

    public class MonthlyTrendDto
    {
        public string? Month { get; set; }
        public decimal Amount { get; set; }
    }

    public class ReceiptReportDto
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public decimal Amount { get; set; }
        public DateTime ReceiptDate { get; set; }
        public string? ReceiptMode { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class ReceiptReportRequestDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Guid? CustomerId { get; set; }
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "ReceiptDate";
        public string SortOrder { get; set; } = "desc";
    }
}

using System;
using System.Collections.Generic;
using Inventory.Application.SalesInvoices.DTOs;
using MediatR;

namespace Inventory.Application.SalesInvoices.Queries
{
    public class UnifiedSalesPagedResultDto
    {
        public List<UnifiedSaleDto> Data { get; set; } = new();
        public int TotalCount { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public int TodayCount { get; set; }
        public int MonthCount { get; set; }
    }

    public class GetUnifiedSalesInvoicesQuery : IRequest<UnifiedSalesPagedResultDto>
    {
        public string SearchTerm { get; set; } = string.Empty;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "Date";
        public string SortOrder { get; set; } = "desc";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? BranchId { get; set; }
        public string? SourceFilter { get; set; } // "All", "QuickSale", "TaxInvoice"
        public bool? IsQuick { get; set; }
    }
}

using Inventory.Application.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PurchaseOrders.DTOs
{
    public class GetPurchaseOrdersRequest
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public string? SortField { get; set; }
        public string? SortOrder { get; set; }

        // Naya Date Range Filter
        public string? Filter { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public bool IsQuick { get; set; }
        public string? BranchId { get; set; }
        public List<FilterDto>? Filters { get; set; }
    }
}

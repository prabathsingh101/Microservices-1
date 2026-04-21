using System.Collections.Generic;

namespace Identity.Application.Common.Models
{
    public class GridRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortColumn { get; set; }
        public string? SortOrder { get; set; } // "asc" or "desc"
    }

    public class GridResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
    }
}

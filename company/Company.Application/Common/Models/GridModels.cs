using System.Collections.Generic;

namespace Company.Application.Common.Models
{
    public sealed class GridRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
        public Dictionary<string, string>? Filters { get; set; }
    }

    public class GridResponse<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
    }
}

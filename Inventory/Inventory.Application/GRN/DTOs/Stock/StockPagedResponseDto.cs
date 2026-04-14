using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.GRN.DTOs.Stock
{
    public class StockPagedResponseDto
    {
        public List<StockSummaryDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}

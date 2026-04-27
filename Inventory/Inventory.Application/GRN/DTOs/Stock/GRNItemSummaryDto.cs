using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.GRN.DTOs.Stock
{
    public class GRNItemSummaryDto
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal OrderedQty { get; set; } // NAYA
        public decimal PendingQty { get; set; } // NAYA
        public decimal ReceivedQty { get; set; }
        public decimal AcceptedQty { get; set; } // NAYA
        public decimal RejectedQty { get; set; } // Total
        public decimal ActualRejectedQty { get; set; }
        public decimal ExpiredQty { get; set; }
        public decimal UnitRate { get; set; }
        public string? RackName { get; set; }
        public bool IsExpired { get; set; }
    }
}

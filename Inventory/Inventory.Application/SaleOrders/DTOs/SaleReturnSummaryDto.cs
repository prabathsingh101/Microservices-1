using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.SaleOrders.DTOs
{
    public class SaleReturnSummaryDto
    {
        public int TotalReturnsToday { get; set; }
        public decimal TotalRefundValue { get; set; }
        public decimal StockRefilledPcs { get; set; }
        public int ConfirmedReturns { get; set; }
        public int PendingInwardCount { get; set; }
    }
}

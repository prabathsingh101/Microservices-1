using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.SaleOrders.DTOs
{
    public class StockExportDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal TotalReceived { get; set; }
        public decimal TotalRejected { get; set; }
        public decimal AvailableStock { get; set; }
    }
}

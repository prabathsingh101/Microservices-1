using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.GRN.DTOs.Stock
{
    public class StockExportDTO
    {
        public string ProductName { get; set; } = "";
        public decimal TotalReceived { get; set; }
        public decimal TotalRejected { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal LastRate { get; set; }
    }
}

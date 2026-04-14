using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Stock
{
    public class StockMovementDto
    {
        public string Product { get; set; }
        public string Type { get; set; } // Purchase ya Sale
        public decimal Qty { get; set; }
        public DateTime? Date { get; set; }
        public string Status { get; set; } // Draft, Submitted, Approved
    }
}

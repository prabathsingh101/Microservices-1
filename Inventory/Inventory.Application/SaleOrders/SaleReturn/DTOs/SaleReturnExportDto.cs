using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.SaleOrders.SaleReturn.DTOs
{
    public class SaleReturnExportDto
    {
        public string ReturnNumber { get; set; }
        public string ReturnDate { get; set; }
        public string CustomerName { get; set; }
        public string SONumber { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public bool IsQuick { get; set; }
    }
}

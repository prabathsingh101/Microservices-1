using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.SaleOrders.DTOs
{
    public class UpdateStatusDto
    {
        public int OrderId { get; set; }
        public string NewStatus { get; set; } = string.Empty; // "Confirmed"
    }
}

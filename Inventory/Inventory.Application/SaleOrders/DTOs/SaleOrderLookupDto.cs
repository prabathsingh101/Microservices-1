using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.SaleOrders.DTOs
{
    public class SaleOrderLookupDto
    {
        // Backend ID jo dropdown ki value banegi [cite: 2026-02-05]
        public Guid SaleOrderId { get; set; }

        // Order Number (e.g., SO-001) jo user ko dropdown mein dikhega
        public string SoNumber { get; set; } = string.Empty;

        public decimal GrandTotal { get; set; }
    }
}

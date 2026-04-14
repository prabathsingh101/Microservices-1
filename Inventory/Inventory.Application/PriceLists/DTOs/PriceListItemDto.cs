using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PriceLists.DTOs
{
    public class PriceListItemDto
    {
        // Product ki unique ID (GUID ya Int jo bhi aap use kar rahe hain) [cite: 2026-01-22]
        public Guid ProductId { get; set; }

        // Table mein dikhane ke liye product ka naam
        public string ProductName { get; set; }

        // Is Price List mein is product ka specific rate
        public decimal Rate { get; set; }

        // Unit (PCS, Mtr, Coil etc.) jo unit column mein jayega
        public string Unit { get; set; }

        // Optional: Agar aapko GST percent bhi price list se hi uthana hai
        public decimal? GstPercent { get; set; }
    }
}

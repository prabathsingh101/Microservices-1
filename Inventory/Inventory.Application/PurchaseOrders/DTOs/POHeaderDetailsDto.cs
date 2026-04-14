using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PurchaseOrders.DTOs
{
    public class POHeaderDetailsDto
    {
        // --- Supplier Info (Microservice Reference) ---
        public Guid SupplierId { get; set; }
        public Guid ProductId { get; set; }
        public string SupplierName { get; set; }      // Display name for the header

        // --- Pricing & Terms ---
        public Guid? PriceListId { get; set; }  
    
        public string Remarks { get; set; }           // 'Remarks / Payment Terms'

        // --- Identification ---
        public Guid PurchaseOrderId { get; set; }     // Internal reference
        public string PoNumber { get; set; }          // e.g., PO/26-27/0034

        // --- Dates ---
        public DateTime PoDate { get; set; }          // 1/28/2026
        public DateTime? ExpectedDeliveryDate { get; set; } // 'Expected Delivery'
    }
    public class RefillItemDto
    {
        public Guid ProductId { get; set; } // GUID [cite: 2026-01-22]
        public string ProductName { get; set; }
        public string Unit { get; set; }
        public decimal Rate { get; set; }
        public decimal GstPercent { get; set; }
    }
}

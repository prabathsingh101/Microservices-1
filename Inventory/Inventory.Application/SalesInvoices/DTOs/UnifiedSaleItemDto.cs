using System;

namespace Inventory.Application.SalesInvoices.DTOs
{
    public class UnifiedSaleItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
        public DateTime? MfgDate { get; set; }
        public DateTime? ExpDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Rack { get; set; } = string.Empty;
    }
}

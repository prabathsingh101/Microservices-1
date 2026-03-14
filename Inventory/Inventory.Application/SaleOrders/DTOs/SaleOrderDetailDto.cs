using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.SaleOrders.DTOs
{
    public class SaleOrderDetailDto
    {
        public int Id { get; set; }
        public string SoNumber { get; set; } = string.Empty;
        public DateTime SoDate { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "Loading..."; // Placeholder for Microservice data
        public string Status { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }
        public decimal SubTotal {  get; set; }

        public decimal TotalTax {  get; set; }
        public string Remarks { get; set; } = string.Empty;
        public DateTime? ExpectedDeliveryDate { get; set; }

        // Order ke andar ke items
        public List<SaleOrderItemDto> Items { get; set; } = new();
    } 
}

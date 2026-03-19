using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Products.DTOs
{
    public class ProductSearchResponseDto
    {
        public Guid id { get; set; }
        public string? name { get; set; }
        public bool? isActive { get; set; }    
        public decimal basePurchasePrice { get; set; }
        public string unit { get; set; }
        public string brand { get; set; }
        public string sku { get; set; }
        public string hsncode { get; set; }
        public decimal mrp { get; set; }
        public decimal defaultGst { get; set; }
        public decimal saleRate { get; set; } 
        public decimal currentStock { get; set; } 
        public string? defaultRackName { get; set; }

        public decimal gstPercent { get; set; } = 0;
        public decimal discountPercent { get; set; }
        public bool isExpiryRequired { get; set; }
        public DateTime? manufacturingDate { get; set; }
        public DateTime? expiryDate { get; set; }
        public string? imageUrl { get; set; }
    }
}

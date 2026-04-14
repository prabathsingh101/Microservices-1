using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Products.DTOs
{   

    public sealed class ProductLookupDto
    {
        public List<CategoryLookupDto> Categories { get; set; } = new();
        public List<SubcategoryLookupDto> Subcategories { get; set; } = new();
    }

    public sealed class CategoryLookupDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string categoryCode { get; set; } = string.Empty;
    }

    public sealed class SubcategoryLookupDto
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

}

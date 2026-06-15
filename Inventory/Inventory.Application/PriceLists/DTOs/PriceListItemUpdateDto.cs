using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PriceLists.DTOs
{
    public record PriceListItemUpdateDto
    {
        public Guid productId { get; init; }
        public decimal rate { get; init; }
        public decimal discountPercent { get; init; }
        public decimal discount { get; init; }
        public int minQty { get; init; }
        public int maxQty { get; init; }
        public string unit { get; init; }
    }
}

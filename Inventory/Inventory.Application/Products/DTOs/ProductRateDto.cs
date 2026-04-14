using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Products.DTOs
{

    public record ProductRateDto(
         Guid ProductId,
         Guid? PriceListId, // Nullable kyunki zaruri nahi har baar price list ho
         decimal PriceListRate, // Specific PriceList se aaya hua rate
         decimal BasePurchasePrice, // Product Master wala fallback rate
         string Unit, // Unit name (e.g., PCS, BOX)
         decimal GstPercent, // Product ka default GST %
         string? HsnCode, // Tax compliance ke liye
         decimal DiscountPercent
     )
    {
        // Helper Property: Agar PriceListRate 0 hai toh BasePrice bhejo
        public decimal RecommendedRate => PriceListRate > 0 ? PriceListRate : BasePurchasePrice;
    }
}

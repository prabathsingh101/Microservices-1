namespace Inventory.Application.PriceLists.DTOs;

// Poore Price List ki detail fetch karne ke liye DTO
public sealed class PriceListDto
{
    // --- Header Details ---
    public Guid id { get; set; }
    public string name { get; set; } = string.Empty;
    public string code { get; set; } = string.Empty;
    public string priceType { get; set; } = string.Empty;
    public string applicableGroup { get; set; } = string.Empty; // UI: Apply to Group
    public string currency { get; set; } = string.Empty;
    public string? remarks { get; set; } // UI: Description/Remarks

    // --- Dates & Status ---
    public DateTime validFrom { get; set; }
    public DateTime? validTo { get; set; }
    public bool isActive { get; set; }

    // --- Audit Info ---
    public DateTime? createdOn { get; set; }
    public string? createdBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public string? ModifiedBy { get; set; }

    // --- Table Items (Details) ---
    // Niche table ki rows ko collect karne ke liye list
    public List<PriceListItemDetailDto> items { get; set; } = new();
}

// Table ki single row fetch karne ke liye DTO
public sealed class PriceListItemDetailDto
{
    public Guid id { get; set; }
    public Guid productId { get; set; }
    public string productName { get; set; } = string.Empty; // UI mein dikhane ke liye
    public string unit { get; set; } = string.Empty; // UI: Unit
    public decimal price { get; set; } // UI: Rate
    public decimal rate { get; set; } // UI: Rate
    public decimal discountPercent { get; set; } // UI: Disc (%)
    public decimal discount { get; set; } // UI: Disc (₹)
    public decimal gstPercent { get; set; } // UI: GST (%)
    public int minQty { get; set; }
    public int maxQty { get; set; }
}

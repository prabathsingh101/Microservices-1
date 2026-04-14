namespace Inventory.Application.PriceLists.DTOs;

public sealed class CreatePriceListDto
{
    public string name { get; set; } = string.Empty;
    public string code { get; set; } = string.Empty;
    public string priceType { get; set; } = "SALES";
    public string applicableGroup { get; set; } = "ALL";
    public string currency { get; set; } = "INR";
    public DateTime validFrom { get; set; }
    public DateTime? validTo { get; set; }
    public string? remarks { get; set; }
    public bool isActive { get; set; }
    public string CreatedBy { get; set; }

    // Angular ke "priceListItems" array se exact match
    public List<CreatePriceListItemDto> priceListItems { get; set; } = new();
}

public sealed class CreatePriceListItemDto
{
    public Guid productId { get; set; }
    //public decimal price { get; set; }
    public decimal rate { get; set; }
    public string unit { get; set; }
    public decimal discountPercent { get; set; }
    public int minQty { get; set; }
    public int maxQty { get; set; }
}

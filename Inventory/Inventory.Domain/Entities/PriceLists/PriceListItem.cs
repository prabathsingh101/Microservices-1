using Inventory.Domain.Entities;

namespace Inventory.Domain.PriceLists;

public class PriceListItem
{
    public Guid Id { get;  set; }
    public Guid PriceListId { get;  set; }
    public virtual PriceList PriceList { get; set; } = null!;
    public Guid ProductId { get;  set; }
    public Product Product { get;  set; }    
    public decimal Rate { get;  set; }
    public string Unit { get; set; }
    public decimal DiscountPercent { get;  set; } // UI: Disc (%)
    public int MinQty { get;  set; }
    public int MaxQty { get;  set; }


    public PriceListItem() { }

    public PriceListItem(Guid priceListId, Guid productId, decimal rate, string unit,
                         decimal discountPercent, int minQty, int maxQty)
    {
        Id = Guid.NewGuid();
        PriceListId = priceListId;
        ProductId = productId;
        Rate = rate;
        Unit = unit;
        DiscountPercent = discountPercent;
        MinQty = minQty;
        MaxQty = maxQty;
    }
}

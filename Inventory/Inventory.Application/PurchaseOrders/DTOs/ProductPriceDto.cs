public class ProductPriceDto
{
    public Guid ProductId { get; set; }
    public decimal Rate { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal GstPercent { get; set; } // Map to DefaultGst from Products
    public decimal DiscountPercent { get; set; } // From Price List
}

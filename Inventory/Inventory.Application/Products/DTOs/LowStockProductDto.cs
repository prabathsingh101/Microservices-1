public class LowStockProductDto
{
    public Guid Id { get; set; }
    public string CategoryName { get; set; } // Category table se join karke
    public string SubCategoryName { get; set; }
    public string ProductName { get; set; }
    public string SKU { get; set; }
    public string Unit { get; set; }
    public decimal CurrentStock { get; set; }
    public int MinStock { get; set; }
    public decimal BasePurchasePrice { get; set; }
}

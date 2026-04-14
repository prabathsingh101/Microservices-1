public class RecentActivityDto
{
    public string Product { get; set; } = string.Empty; // Product Name
    public string Type { get; set; } = string.Empty;    // "Sale" or "Purchase"
    public decimal Qty { get; set; }                   // Quantity
    public DateTime Date { get; set; }                 // Transaction Date
    public string Status { get; set; } = string.Empty; // Completed, Pending, etc.
}

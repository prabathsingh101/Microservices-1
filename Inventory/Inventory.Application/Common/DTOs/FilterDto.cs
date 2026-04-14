namespace Inventory.Application.Common.DTOs
{
    public class FilterDto
    {
        public string Field { get; set; } = string.Empty; // Column name (e.g., "orderNo")
        public string Value { get; set; } = string.Empty; // Search text (e.g., "PO-101")
    }
}

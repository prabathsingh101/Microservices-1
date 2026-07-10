namespace Inventory.Application.Categories.DTOs;

public sealed class CategoryDto
{
    public Guid id { get; set; }
    public Guid? companyId { get; set; }
    public string categoryName { get; set; } = string.Empty;
    public string categoryCode { get; set; } = string.Empty;
    public decimal defaultGst { get; set; }
    public string? description { get; set; }
    public bool isActive { get; set; }
    public string industryType { get; set; } = "General";
}

using Inventory.Domain.Entities;

namespace Inventory.Application.Subcategories.DTOs;

public sealed class SubcategoryDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public string SubcategoryCode { get; set; } = string.Empty;
    public string SubcategoryName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }=DateTime.Now;  
    public DateTime? ModifiedOn { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public decimal DefaultGst { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

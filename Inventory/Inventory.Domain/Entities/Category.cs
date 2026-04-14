namespace Inventory.Domain.Entities;

public class Category : Inventory.Domain.Common.BaseAuditableEntity
{
    public Guid Id { get; private set; }
    public string? CategoryCode { get; set; } = null!;
    public string? CategoryName { get; set; } = null!;
    public decimal DefaultGst { get;  set; }
    public string? Description { get;  set; }
    public bool IsActive { get;  set; }

    // --- Subcategory Logic Start ---
    public Guid? ParentCategoryId { get;  set; } // Nullable Guid
    public virtual Category? ParentCategory { get;  set; } // Self-referencing property
    public virtual ICollection<Category> SubCategories { get;  set; } = new List<Category>();
    // --- Subcategory Logic End ---

    public Category() { } // EF Core

    public Category(
        string name,
        string? code,
        decimal defaultGst,
        string? description,
        bool isActive,
        Guid? parentCategoryId = null)
    {
        Id = Guid.NewGuid();
        CategoryName = name;
        CategoryCode = code;
        DefaultGst = defaultGst;
        Description = description;
        IsActive = isActive;
        ParentCategoryId = parentCategoryId;
    }

    public void Update(
        string name,
        string? code,
        decimal defaultGst,
        string? description,
        bool isActive,
        Guid? parentCategoryId = null) 
    {
        CategoryName = name;
        CategoryCode = code;
        DefaultGst = defaultGst;
        Description = description;
        IsActive = isActive;
        ParentCategoryId = parentCategoryId;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

using Inventory.Domain.Entities;

public sealed class ProductDto
{
    public Guid id { get; set; }
    public Guid? companyId { get; set; }
    public Guid categoryId { get; set; }    
    public Guid subcategoryId { get; set; }
    public string categoryName { get; set; } = string.Empty;
    public string subcategoryName { get; set; } = string.Empty;

    public string productName { get; set; } = string.Empty;
    public string? sku { get;  set; }
    public string? brand { get; set; }
    public string unit { get; set; } = string.Empty;
    public decimal? basePurchasePrice { get; set; }
    public decimal? mrp { get; set; }
    public decimal? saleRate { get; set; }
    public string? hsnCode { get; set; }
    public decimal? defaultGst { get; set; }
    public int minStock { get; set; }
    public decimal currentStock { get; set; }
    public decimal damagedStock { get; set; }
    public int productType { get; set; }
    public bool trackInventory { get; set; }
    public bool? isActive { get; set; }
    public bool isExpiryRequired { get; set; }
    public string? description { get; set; }
    public string? createdBy { get; set; }
    public DateTime? createdOn { get; set; } = DateTime.Now;
    public DateTime? modifiedOn { get; set; } = DateTime.UtcNow;
    public string? modifiedBy { get; set; }
    public Guid? defaultWarehouseId { get; set; }
    public string? defaultWarehouseName { get; set; }
    public Guid? defaultRackId { get; set; }
    public string? defaultRackName { get; set; }
    public string? imageUrl { get; set; }
    public decimal discount { get; set; }
    public decimal discountPercent {  get; set; }
    public DateTime? manufacturingDate { get; set; }
    public DateTime? expiryDate { get; set; }
    public string? genericName { get; set; }
    public string? manufacturer { get; set; }
    public string? scheduleClass { get; set; }
}

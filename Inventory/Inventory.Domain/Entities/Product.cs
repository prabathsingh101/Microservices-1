namespace Inventory.Domain.Entities;

public class Product : Inventory.Domain.Common.BaseAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; }
    public Guid SubcategoryId { get; private set; }
    public Subcategory Subcategory { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Sku { get; private set; }
    public string? Brand { get; private set; } 

    public string Unit { get; private set; } = null!;

    public decimal BasePurchasePrice { get; set; }=0;
    public decimal MRP { get; set; }=0;
    public decimal Discount { get; set; } = 0;
    public decimal? SaleRate { get; set; }

    public decimal? DefaultGst { get; set; }
    public string HSNCode { get; private set; } = null!;
    public int MinStock { get;  set; } = 0;
    public decimal CurrentStock { get; set; } = 0;
    public bool TrackInventory { get; private set; }    
    public bool IsActive { get;  set; }
    public string? Description { get; private set; }
    
    // Audit fields are now in BaseAuditableEntity

    public string ProductType { get; set; } = string.Empty;
    public decimal DamagedStock { get; set; } = 0;
    public bool IsExpiryRequired { get; set; } = false;

    public Guid? DefaultWarehouseId { get; set; }
    public virtual Warehouse? DefaultWarehouse { get; set; }
    public Guid? DefaultRackId { get; set; }
    public virtual Rack? DefaultRack { get; set; }
    public string? ImageUrl { get; set; }

    private Product() { }

    public Product(
        Guid categoryid,
        Guid subcategoryid,
        string productname,
        string sku,
        string brand,
        string unit,
        string hsncode,
        decimal basepurchaseprice,
        decimal mrp,
        decimal discount,
        decimal defaultgst,
        int minstock,
        bool trackinventory,
        bool isactive,
        string? description,
        string? createdby,
        decimal saleRate,
        string productType,
        decimal damagedStock,
        Guid? defaultWarehouseId = null,
        Guid? defaultRackId = null,
        bool isExpiryRequired = false,
        string? imageUrl = null,
        Guid companyId = default
        )
    {
        Id = Guid.NewGuid();
        CategoryId = categoryid;
        SubcategoryId = subcategoryid;
        Name = productname;
        Sku = sku;
        Discount = discount;
        SaleRate = saleRate;
        Brand = brand;
        Unit = unit;
        HSNCode = hsncode; 
        BasePurchasePrice = basepurchaseprice;
        MRP = mrp;
        DefaultGst = defaultgst;
        MinStock = minstock;        
        TrackInventory = trackinventory;
        IsActive = isactive;
        Description = description;
        CreatedBy = createdby;
        ProductType = productType;
        DamagedStock = damagedStock;
        DefaultWarehouseId = defaultWarehouseId;
        DefaultRackId = defaultRackId;
        IsExpiryRequired = isExpiryRequired;
        CompanyId = companyId;
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? "/assets/images/placeholder-product.png" : imageUrl;
    }

    public void Update(        
        Guid categoryid,
        Guid subcategoryid,
        string name,
        string sku,
        decimal saleRate,
        decimal discount,
        string brand,
        string unit,
        string hsncode,
        decimal basepurchaseprice,
        decimal mrp,
        decimal defaultGst,
        int minstock,
        bool trackinventory,
        bool isactive,
        string? description,
        string? ModifiedBy,
        string ? productType,
        decimal damagedStock,
        Guid? defaultWarehouseId = null,
        Guid? defaultRackId = null,
        bool isExpiryRequired = false,
        string? imageUrl = null,
        DateTime? modifiedon = null,
        Guid companyId = default
        )
    {
        CategoryId = categoryid;
        SubcategoryId = subcategoryid;
        Name = name;
        Sku = sku;
        Discount = discount;
        SaleRate = saleRate;
        Brand = brand;        
        Unit = unit;
        HSNCode = hsncode;
        BasePurchasePrice = basepurchaseprice;
        MRP = mrp;
        DefaultGst = defaultGst;
        MinStock = minstock;
        TrackInventory = trackinventory;
        IsActive = isactive;
        Description = description;    
        ModifiedBy = ModifiedBy;
        ProductType = productType; 
        DamagedStock = damagedStock;
        DefaultWarehouseId = defaultWarehouseId;
        DefaultRackId = defaultRackId;
        IsExpiryRequired = isExpiryRequired;
        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            ImageUrl = imageUrl;
        }
        else if (string.IsNullOrWhiteSpace(ImageUrl)) 
        {
             ImageUrl = "/assets/images/placeholder-product.png";
        }
        CompanyId = companyId;
        ModifiedOn = modifiedon ?? DateTime.UtcNow;       
    }
}

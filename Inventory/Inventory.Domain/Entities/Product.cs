using Inventory.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
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
    public decimal? SaleRate { get; set; }
    public decimal? DefaultGst { get; set; }
    public string HSNCode { get; private set; } = null!;
    public int MinStock { get;  set; } = 0;
    public decimal CurrentStock { get; set; } = 0;
    public bool TrackInventory { get; private set; }    
    public bool IsActive { get;  set; }
    public string? Description { get; private set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public decimal DamagedStock { get; set; } = 0;
    public bool IsExpiryRequired { get; set; } = false;

    public Guid? DefaultWarehouseId { get; set; }
    public virtual Warehouse? DefaultWarehouse { get; set; }
    public Guid? DefaultRackId { get; set; }
    public virtual Rack? DefaultRack { get; set; }

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
        decimal defaultgst,
        int minstock,
        bool trackinventory,
        bool isactive,
        string? description,
        string createdby,
        decimal saleRate,
        string productType,
        decimal damagedStock,
        Guid? defaultWarehouseId = null,
        Guid? defaultRackId = null,
        bool isExpiryRequired = false
        )
    {
        Id = Guid.NewGuid();
        CategoryId = categoryid;
        SubcategoryId = subcategoryid;
        Name = productname;
        Sku = sku;
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
    }

    public void Update(        
        Guid categoryid,
        Guid subcategoryid,
        string name,
        string sku,
        decimal saleRate,
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
        string updatedby,
       string ? productType,
       decimal damagedStock,
       Guid? defaultWarehouseId = null,
       Guid? defaultRackId = null,
       bool isExpiryRequired = false,
        DateTime? modifiedon = null
        
        )
    {
        CategoryId = categoryid;
        SubcategoryId = subcategoryid;
        Name = name;
        Sku = sku;
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
        ModifiedBy = updatedby;
        ProductType = productType; 
        DamagedStock = damagedStock;
        DefaultWarehouseId = defaultWarehouseId;
        DefaultRackId = defaultRackId;
        IsExpiryRequired = isExpiryRequired;
        ModifiedOn = modifiedon ?? DateTime.UtcNow;       
    }
}

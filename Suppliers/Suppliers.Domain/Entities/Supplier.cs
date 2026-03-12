public class Supplier
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Phone { get; private set; }
    public string? GstIn { get; private set; }
    public string? Address { get; private set; }
    public string? Email { get; private set; }

    // Naya Field: DDD logic ke liye private set rakhein
    public Guid? DefaultPriceListId { get; private set; } // Type Guid? fix kiya gaya

    public bool IsActive { get; private set; } = true;
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; } = DateTime.Now;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; } = DateTime.Now;

    private Supplier() { Name = null!; Phone = null!; }

    // Constructor Update
    public Supplier(
        string name,
        string phone,
        string? gstin,
        string? address,
        string? email,
        string? createdBy,
        bool isActive,
        Guid? defaultPriceListId = null // Guid? type use karein
        )
    {
        Name = name;
        Phone = phone;
        GstIn = gstin;
        Address = address;
        Email = email;
        CreatedBy = createdBy;
        IsActive = isActive;
        DefaultPriceListId = defaultPriceListId;
        CreatedDate = DateTime.Now;
    }

    // Method to update price list (DDD Business Rule)
    public void SetDefaultPriceList(Guid? priceListId) // Guid? parameter update
    {
        // Business logic: Ensure GUID is not empty
        if (priceListId == Guid.Empty) throw new ArgumentException("Invalid Price List ID");

        DefaultPriceListId = priceListId;
        UpdatedDate = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string phone, string? gstIn, string? address, string? email, bool isActive, Guid? defaultPriceListId)
    {
        Name = name;
        Phone = phone;
        GstIn = gstIn;
        Address = address;
        Email = email;
        IsActive = isActive;
        
        if (defaultPriceListId.HasValue && defaultPriceListId == Guid.Empty)
             throw new ArgumentException("Invalid Price List ID");

        DefaultPriceListId = defaultPriceListId;
        UpdatedDate = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
}
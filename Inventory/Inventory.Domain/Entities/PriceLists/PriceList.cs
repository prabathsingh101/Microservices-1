namespace Inventory.Domain.PriceLists;

public class PriceList : Inventory.Domain.Common.BaseAuditableEntity
{
    public Guid Id { get;  set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; }
    public string Code { get;  set; }
    public string PriceType { get;  set; }
    public string ApplicableGroup { get;  set; } // UI: Apply to Group
    public string Currency { get;  set; }        // UI: Currency
    public string? Remarks { get;  set; }        // UI: Description/Remarks
    public DateTime ValidFrom { get;  set; }
    public DateTime? ValidTo { get;  set; }
    public bool IsActive { get;  set; }

    // Relationship
    public List<PriceListItem> PriceListItems { get;  set; } = new();

    private PriceList() { } // EF Core ke liye

    public PriceList(string name, string code, string priceType, string applicableGroup,
                     string currency, DateTime validFrom, DateTime? validTo,
                     string? remarks, bool isActive, string createdBy, Guid companyId)
    {
        Id = Guid.NewGuid();
        Name = name;
        Code = code;
        PriceType = priceType;
        ApplicableGroup = applicableGroup;
        Currency = currency;
        ValidFrom = validFrom;
        ValidTo = validTo;
        Remarks = remarks;
        IsActive = isActive;
        CreatedBy = createdBy;
        CompanyId = companyId;
    }
}

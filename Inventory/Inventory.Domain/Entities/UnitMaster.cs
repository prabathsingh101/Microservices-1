using Inventory.Domain.Common;

public class UnitMaster : BaseAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } // e.g., Kg, Litre
    public string Description { get; private set; }
    public bool IsActive { get; private set; }

    public UnitMaster(string name, string description, Guid companyId)
    {
        Id = Guid.NewGuid();
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required");
        Name = name;
        Description = description;
        IsActive = true;
        CompanyId = companyId;
    }

    public void Update(string name, string description, bool isActive, Guid companyId)
    {
        Name = name;
        Description = description;
        IsActive = isActive;
        CompanyId = companyId;
    }
}

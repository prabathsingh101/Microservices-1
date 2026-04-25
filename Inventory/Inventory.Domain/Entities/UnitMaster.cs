using Inventory.Domain.Common;

public class UnitMaster : BaseAuditableEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } // e.g., Kg, Litre
    public string Description { get; private set; }
    public bool IsActive { get; private set; }

    public UnitMaster(string name, string description, Guid companyId, string? branchId = null)
    {
        Id = Guid.NewGuid();
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required");
        Name = name;
        Description = description;
        IsActive = true;
        CompanyId = companyId;
        BranchId = branchId;
    }

    public void Update(string name, string description, bool isActive, Guid companyId, string? branchId = null)
    {
        Name = name;
        Description = description;
        IsActive = isActive;
        CompanyId = companyId;
        BranchId = branchId;
    }
}

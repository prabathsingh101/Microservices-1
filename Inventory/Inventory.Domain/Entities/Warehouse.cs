using Inventory.Domain.Common;

namespace Inventory.Domain.Entities;

public class Warehouse : BaseAuditableEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? City { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public virtual ICollection<Rack> Racks { get; private set; } = new List<Rack>();

    private Warehouse() { }

    public Warehouse(string name, string? city, string? description, bool isActive, Guid companyId, string? branchId = null)
    {
        Id = Guid.NewGuid();
        Name = name;
        City = city;
        Description = description;
        IsActive = isActive;
        CompanyId = companyId;
        BranchId = branchId;
    }

    public void Update(string name, string? city, string? description, bool isActive, Guid companyId, string? branchId = null)
    {
        Name = name;
        City = city;
        Description = description;
        IsActive = isActive;
        CompanyId = companyId;
        BranchId = branchId;
    }
}

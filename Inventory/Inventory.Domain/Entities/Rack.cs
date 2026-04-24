using Inventory.Domain.Common;

namespace Inventory.Domain.Entities;

public class Rack : BaseAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public virtual Warehouse Warehouse { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private Rack() { }

    public Rack(Guid warehouseId, string name, string? description, bool isActive, Guid companyId, string? branchId = null)
    {
        Id = Guid.NewGuid();
        WarehouseId = warehouseId;
        Name = name;
        Description = description;
        IsActive = isActive;
        CompanyId = companyId;
        BranchId = branchId;
    }

    public void Update(Guid warehouseId, string name, string? description, bool isActive, Guid companyId, string? branchId = null)
    {
        WarehouseId = warehouseId;
        Name = name;
        Description = description;
        IsActive = isActive;
        CompanyId = companyId;
        BranchId = branchId;
    }
}

using Inventory.Domain.Common;

public class UnitMaster : BaseAuditableEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } // e.g., Kg, Litre
    public string Description { get; private set; }
    public bool IsActive { get; private set; }

    public UnitMaster(string name, string description)
    {
        Id = Guid.NewGuid();
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required");
        Name = name;
        Description = description;
        IsActive = true;
    }

    public void Update(string name, string description, bool isActive)
    {
        Name = name;
        Description = description;
        IsActive = isActive;
    }
}

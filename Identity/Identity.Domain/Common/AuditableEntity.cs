namespace Identity.Domain.Common;

public abstract class AuditableEntity : IMultiTenant
{
    public Guid? CompanyId { get; set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; protected set; }

    public void SetModified()
    {
        ModifiedAt = DateTime.UtcNow;
    }
}

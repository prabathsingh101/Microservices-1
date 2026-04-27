using Identity.Domain.Users;
using Identity.Domain.Common;

namespace Identity.Domain.Entities;

public class RefreshToken : AuditableEntity, IMultiTenant
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }

    // FK
    public Guid UserId { get; private set; }
    public Guid? CompanyId { get; set; }
    public string? BranchId { get; set; }

    // Navigation (optional)
    public User? User { get; private set; }

    private RefreshToken() { } // EF Core

    public RefreshToken(Guid userId, string token, DateTime expiresAt, Guid? companyId = null, string? branchId = null)
    {
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        IsRevoked = false;
        CompanyId = companyId;
        BranchId = branchId;
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsExpired && !IsRevoked;

    // ✅ REQUIRED METHOD (Updated with Audit support)
    public void Revoke(string? revokedBy = null)
    {
        IsRevoked = true;
        if (!string.IsNullOrEmpty(revokedBy))
        {
            LastModifiedBy = revokedBy;
            LastModifiedDate = DateTime.UtcNow;
        }
    }
}

using Identity.Domain.Users;

namespace Identity.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }

    // FK
    public Guid UserId { get; private set; }
    public Guid? CompanyId { get; private set; }

    // Navigation (optional)
    public User? User { get; private set; }

    private RefreshToken() { } // EF Core

    public RefreshToken(Guid userId, string token, DateTime expiresAt, Guid? companyId = null)
    {
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        IsRevoked = false;
        CompanyId = companyId;
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsExpired && !IsRevoked;

    // ✅ REQUIRED METHOD (THIS WAS MISSING)
    public void Revoke()
    {
        IsRevoked = true;
    }
}

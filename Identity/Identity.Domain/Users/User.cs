using Identity.Domain.Entities;
using Identity.Domain.Users;

namespace Identity.Domain;

public class User : Identity.Domain.Common.IMultiTenant
{
    private readonly List<UserRole> _userRoles = new();
    private readonly List<RefreshToken> _refreshTokens = new();

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; } // Link to their business organization
    public string UserName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    // Reset Token
    public string? ResetToken { get; private set; }
    public DateTime? ResetTokenExpires { get; private set; }

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User() { }

    public User(string userName, string email)
    {
        UserName = userName;
        Email = email;
    }

    public void SetPasswordHash(string hash)
    {
        PasswordHash = hash;
    }

    public void SetCompanyId(Guid? companyId)
    {
        CompanyId = companyId;
    }

    public void AssignRole(Guid roleId)
    {
        if (_userRoles.Any(r => r.RoleId == roleId))
            return;

        _userRoles.Add(new UserRole(Id, roleId, this.CompanyId));
    }

    // ✅ FIXED
    public void AddRefreshToken(string token, DateTime expiresAt)
    {
        _refreshTokens.Add(new RefreshToken(Id, token, expiresAt, this.CompanyId));
    }

    public void RevokeRefreshToken(string token)
    {
        var rt = _refreshTokens.Single(x => x.Token == token);
        rt.Revoke();
    }

    public void UpdateDetails(string userName, string email, bool isActive)
    {
        UserName = userName;
        Email = email;
        IsActive = isActive;
    }

    public void UpdateRoles(List<Guid> roleIds)
    {
        // 1. Remove roles no longer assigned
        var rolesToRemove = _userRoles.Where(r => !roleIds.Contains(r.RoleId)).ToList();
        foreach (var role in rolesToRemove)
        {
            _userRoles.Remove(role);
        }

        // 2. Add new roles and sync CompanyId
        foreach (var roleId in roleIds)
        {
            var existing = _userRoles.FirstOrDefault(r => r.RoleId == roleId);
            if (existing == null)
            {
                _userRoles.Add(new UserRole(Id, roleId, this.CompanyId));
            }
            else
            {
                existing.CompanyId = this.CompanyId;
            }
        }
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    public void SetResetToken(string token, DateTime expires)
    {
        ResetToken = token;
        ResetTokenExpires = expires;
    }

    public void ClearResetToken()
    {
        ResetToken = null;
        ResetTokenExpires = null;
    }
}

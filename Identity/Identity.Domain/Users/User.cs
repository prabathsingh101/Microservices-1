using Identity.Domain.Common;
using Identity.Domain.Entities;
using Identity.Domain.Users;

namespace Identity.Domain;

public class User : AuditableEntity, Identity.Domain.Common.IMultiTenant
{
    private readonly List<UserRole> _userRoles = new();
    private readonly List<RefreshToken> _refreshTokens = new();

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; } // Link to their business organization
    public string? BranchId { get; set; } // Link to their specific branch
    public string UserName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string? PasswordHash { get; private set; }
    public string AuthProvider { get; private set; } = "local"; // "local" or "google"
    public string? GoogleId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? ProfileImage { get; set; }

    // Extended profile fields (optional)
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Designation { get; private set; }
    public string? Department { get; private set; }
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? Pincode { get; private set; }
    public string? Gender { get; private set; }
    public DateTime? DateOfBirth { get; private set; }
    public string? AadhaarUrl { get; private set; }
    public string? PanCardUrl { get; private set; }

    // Reset Token
    public string? ResetToken { get; private set; }
    public DateTime? ResetTokenExpires { get; private set; }

    // Concurrent Login Protection
    public string? CurrentSessionId { get; private set; }

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User() { }

    public User(string userName, string email)
    {
        UserName = userName;
        Email = email;
    }

    public void SetPasswordHash(string? hash)
    {
        PasswordHash = hash;
    }

    public void SetAuthProvider(string provider)
    {
        AuthProvider = provider;
    }

    public void SetGoogleId(string googleId)
    {
        GoogleId = googleId;
    }

    public void SetCompanyId(Guid? companyId)
    {
        CompanyId = companyId;
    }

    public void SetBranchId(string? branchId)
    {
        BranchId = branchId;
    }

    public void SetProfileImage(string? profileImage)
    {
        ProfileImage = profileImage;
    }

    public void AssignRole(Guid roleId)
    {
        if (_userRoles.Any(r => r.RoleId == roleId))
            return;

        _userRoles.Add(new UserRole(Id, roleId, this.CompanyId, this.BranchId));
    }

    // ✅ FIXED
    public void AddRefreshToken(string token, DateTime expiresAt)
    {
        var refreshToken = new RefreshToken(Id, token, expiresAt, this.CompanyId, this.BranchId);
        
        // 🕒 Manually set audit fields because during login, ICurrentUserService might be null
        refreshToken.CreatedBy = this.Email; // Use user's email as creator during login
        refreshToken.CreatedDate = DateTime.UtcNow;
        
        _refreshTokens.Add(refreshToken);
    }

    public void RevokeRefreshToken(string token)
    {
        var rt = _refreshTokens.Single(x => x.Token == token);
        rt.Revoke(this.Email); // Pass user email as revoker
    }

    public void UpdateDetails(string userName, string email, bool isActive)
    {
        UserName = userName;
        Email = email;
        IsActive = isActive;
    }

    public void UpdateExtendedProfile(
        string? firstName,
        string? lastName,
        string? phoneNumber,
        string? designation,
        string? department,
        string? address,
        string? city,
        string? state,
        string? pincode,
        string? gender,
        DateTime? dateOfBirth,
        string? aadhaarUrl,
        string? panCardUrl)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        Designation = designation;
        Department = department;
        Address = address;
        City = city;
        State = state;
        Pincode = pincode;
        Gender = gender;
        DateOfBirth = dateOfBirth;
        AadhaarUrl = aadhaarUrl;
        PanCardUrl = panCardUrl;
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
                _userRoles.Add(new UserRole(Id, roleId, this.CompanyId, this.BranchId));
            }
            else
            {
                existing.CompanyId = this.CompanyId;
                existing.BranchId = this.BranchId;
            }
        }
    }

    public void ClearRolesCollection()
    {
        _userRoles.Clear();
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

    public void SetCurrentSessionId(string sessionId)
    {
        CurrentSessionId = sessionId;
    }
}

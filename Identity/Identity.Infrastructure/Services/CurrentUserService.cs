using System.Security.Claims;
using Identity.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Identity.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? CompanyId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            // 1. Try Header (X-Company-Id) FIRST - Important for multi-tenant endpoints without JWT (like refresh)
            var headerValue = httpContext.Request.Headers["X-Company-Id"].ToString();
            if (!string.IsNullOrEmpty(headerValue) && headerValue != "null")
            {
                if (Guid.TryParse(headerValue, out var headerId)) return headerId;
            }

            // 2. Fallback to JWT Claim
            var user = httpContext.User;
            var claims = user?.Claims;
            if (claims == null) return null;

            var claimValue = claims.FirstOrDefault(c => 
                c.Type.Equals("CompanyId", StringComparison.OrdinalIgnoreCase) || 
                c.Type.Equals("companyid", StringComparison.OrdinalIgnoreCase))?.Value;

            return Guid.TryParse(claimValue, out var id) ? id : null;
        }
    }

    public string? BranchId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            // 1. Try Header (X-Branch-Id) FIRST - This represents the "Working Context" (Branch Switcher)
            var headerValue = httpContext.Request.Headers["X-Branch-Id"].ToString();
            if (!string.IsNullOrEmpty(headerValue) && headerValue != "null") return headerValue;

            // 2. Fallback to JWT Claim (The user's home branch)
            var claimValue = httpContext.User.Claims.FirstOrDefault(c =>
                c.Type.Equals("BranchId", StringComparison.OrdinalIgnoreCase) ||
                c.Type.Equals("branchid", StringComparison.OrdinalIgnoreCase))?.Value;

            if (!string.IsNullOrEmpty(claimValue)) return claimValue;

            return null;
        }
    }

    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var claim = user?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim?.Value, out var id) ? id : null;
        }
    }

    public bool IsSuperAdmin
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return false;

            // 1. System/Platform Admin check
            var email = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value;
            var companyName = user.Claims.FirstOrDefault(c => c.Type == "CompanyName")?.Value;

            bool isPlatformEmail = email != null && email.Equals("Default_Admin@gmail.com", StringComparison.OrdinalIgnoreCase);
            bool isPlatformCompany = companyName != null && companyName.Equals("Admin Dashboard", StringComparison.OrdinalIgnoreCase);

            // 2. "Super Admin" or "Default Admin" Role check
            bool isSuperAdminRole = user.IsInRole("Super Admin") || 
                                   user.IsInRole("Default Admin") ||
                                   user.Claims.Any(c => c.Type == ClaimTypes.Role && 
                                       (c.Value.Equals("Super Admin", StringComparison.OrdinalIgnoreCase) || 
                                        c.Value.Equals("Default Admin", StringComparison.OrdinalIgnoreCase)));

            return isPlatformEmail || isPlatformCompany || isSuperAdminRole;
        }
    }
}

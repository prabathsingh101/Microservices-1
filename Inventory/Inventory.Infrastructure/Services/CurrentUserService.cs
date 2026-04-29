using System.Security.Claims;
using Inventory.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Inventory.Infrastructure.Services;

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
            var claims = _httpContextAccessor.HttpContext?.User?.Claims;
            if (claims == null) return null;

            // 1. Try different claim names (Case-insensitive)
            var claimValue = claims.FirstOrDefault(c => 
                c.Type.Equals("CompanyId", StringComparison.OrdinalIgnoreCase) || 
                c.Type.Equals("companyid", StringComparison.OrdinalIgnoreCase))?.Value;

            if (Guid.TryParse(claimValue, out var companyId))
            {
                return companyId;
            }

            // 2. Fallback: Try to get from 'X-Company-Id' Header
            var headerValue = _httpContextAccessor.HttpContext?.Request.Headers["X-Company-Id"].ToString();
            if (Guid.TryParse(headerValue, out var headerId))
            {
                return headerId;
            }

            return null;
        }
    }

    public string? BranchId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            // 1. Try Header 'X-Branch-Id' FIRST - Active Working Context
            var headerValue = httpContext.Request.Headers["X-Branch-Id"].ToString();
            if (!string.IsNullOrEmpty(headerValue) && headerValue != "null") return headerValue;

            // 2. Fallback: Try claim 'branchid' or 'BranchId' - Home Branch
            var claims = httpContext.User?.Claims;
            if (claims != null)
            {
                var claimValue = claims.FirstOrDefault(c => 
                    c.Type.Equals("BranchId", StringComparison.OrdinalIgnoreCase) || 
                    c.Type.Equals("branchid", StringComparison.OrdinalIgnoreCase))?.Value;

                if (!string.IsNullOrEmpty(claimValue)) return claimValue;
            }

            return null;
        }
    }

    public string? Email
    {
        get
        {
            var claims = _httpContextAccessor.HttpContext?.User?.Claims;
            if (claims == null) return null;

            return claims.FirstOrDefault(c => 
                c.Type.Equals(ClaimTypes.Email, StringComparison.OrdinalIgnoreCase) || 
                c.Type.Equals("email", StringComparison.OrdinalIgnoreCase))?.Value;
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


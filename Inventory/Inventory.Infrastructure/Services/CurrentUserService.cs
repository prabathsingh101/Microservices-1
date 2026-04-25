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
            var claims = _httpContextAccessor.HttpContext?.User?.Claims;
            if (claims == null) return null;

            // 1. Try claim 'branchid' or 'BranchId'
            var claimValue = claims.FirstOrDefault(c => 
                c.Type.Equals("BranchId", StringComparison.OrdinalIgnoreCase) || 
                c.Type.Equals("branchid", StringComparison.OrdinalIgnoreCase))?.Value;

            if (!string.IsNullOrEmpty(claimValue)) return claimValue;

            // 2. Fallback: Try Header 'X-Branch-Id'
            var headerValue = _httpContextAccessor.HttpContext?.Request.Headers["X-Branch-Id"].ToString();
            if (!string.IsNullOrEmpty(headerValue)) return headerValue;

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
}


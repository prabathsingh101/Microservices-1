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
}

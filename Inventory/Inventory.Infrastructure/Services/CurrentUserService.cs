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
            // 1. Try to get from JWT Claim
            var companyIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("CompanyId");
            if (Guid.TryParse(companyIdClaim, out var companyId))
            {
                return companyId;
            }

            // 2. Fallback: Try to get from 'X-Company-Id' Header (useful for Super Admin switching companies)
            var headerValue = _httpContextAccessor.HttpContext?.Request.Headers["X-Company-Id"].ToString();
            if (Guid.TryParse(headerValue, out var headerId))
            {
                return headerId;
            }

            return null;
        }
    }
}

using Company.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;

namespace Company.Infrastructure.Services
{
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
                // 1. Fallback to Header for Service-to-Service communication (Highest Priority)
                var companyIdHeader = _httpContextAccessor.HttpContext?.Request.Headers["X-Company-Id"].ToString();
                if (!string.IsNullOrEmpty(companyIdHeader) && Guid.TryParse(companyIdHeader, out var hId))
                {
                    return hId;
                }

                // 2. Check Token Claim
                var companyIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("CompanyId");
                if (Guid.TryParse(companyIdClaim, out var companyId))
                {
                    return companyId;
                }

                return null;
            }
        }
        
        public Guid? UserId
        {
            get
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    return userId;
                }
                return null;
            }
        }
    }
}

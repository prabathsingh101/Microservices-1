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
                var companyIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("CompanyId");
                
                // Fallback to Header for Service-to-Service communication
                if (string.IsNullOrEmpty(companyIdClaim))
                {
                    companyIdClaim = _httpContextAccessor.HttpContext?.Request.Headers["X-Company-Id"];
                }

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

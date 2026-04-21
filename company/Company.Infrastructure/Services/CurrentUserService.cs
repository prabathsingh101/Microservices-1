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
                // 1. Fallback to Header for Service-to-Service communication
                var companyIdHeader = _httpContextAccessor.HttpContext?.Request.Headers["X-Company-Id"].ToString();
                if (!string.IsNullOrEmpty(companyIdHeader) && Guid.TryParse(companyIdHeader, out var hId))
                {
                    return hId;
                }

                // 2. Check Token Claim (Explicit search)
                var user = _httpContextAccessor.HttpContext?.User;
                var claim = user?.Claims.FirstOrDefault(c => c.Type == "CompanyId");
                
                if (claim != null && Guid.TryParse(claim.Value, out var companyId))
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
                var user = _httpContextAccessor.HttpContext?.User;
                var userIdClaim = user?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    return userId;
                }
                return null;
            }
        }

        public bool IsSuperAdmin
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;
                if (user == null) return false;

                // 🚀 STRICT PLATFORM ADMIN CHECK
                var email = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value;
                var companyName = user.Claims.FirstOrDefault(c => c.Type == "CompanyName")?.Value;

                bool isPlatformEmail = email != null && email.Equals("Default_Admin@gmail.com", StringComparison.OrdinalIgnoreCase);
                bool isPlatformCompany = companyName != null && companyName.Equals("Admin Dashboard", StringComparison.OrdinalIgnoreCase);

                return isPlatformEmail || isPlatformCompany;
            }
        }
    }
}

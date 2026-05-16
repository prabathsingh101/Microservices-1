using Customers.Application.Common.Interfaces;
using System.Security.Claims;

namespace Customers.API.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public Guid? CompanyId
        {
            get
            {
                var claims = _httpContextAccessor.HttpContext?.User?.Claims;
                if (claims == null) return null;

                // 1. Try different claim names (Case-insensitive) [cite: 2026-04-19]
                var claimValue = claims.FirstOrDefault(c => 
                    c.Type.Equals("CompanyId", StringComparison.OrdinalIgnoreCase) || 
                    c.Type.Equals("companyid", StringComparison.OrdinalIgnoreCase))?.Value;

                if (Guid.TryParse(claimValue, out var claimGuid)) return claimGuid;

                // 2. Fallback: Request Header (Important for Super Admin or Service-to-Service)
                var headerValue = _httpContextAccessor.HttpContext?.Request.Headers["X-Company-Id"].ToString();
                if (Guid.TryParse(headerValue, out var headerGuid)) return headerGuid;

                return null;
            }
        }

        public string? BranchId
        {
            get
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null) return null;

                // 1. Fallback to Header (X-Branch-Id) - Check this first!
                var headerValue = httpContext.Request.Headers["X-Branch-Id"].ToString();
                if (!string.IsNullOrEmpty(headerValue) && headerValue != "null") return headerValue;

                // 2. Try JWT Claim
                var claimValue = httpContext.User.Claims.FirstOrDefault(c =>
                    c.Type.Equals("BranchId", StringComparison.OrdinalIgnoreCase) ||
                    c.Type.Equals("branchid", StringComparison.OrdinalIgnoreCase))?.Value;

                if (!string.IsNullOrEmpty(claimValue)) return claimValue;

                return null;
            }
        }
    }
}

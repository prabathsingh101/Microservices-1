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
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null) return null;

                // 1. Try X-Company-Id header first for company context switching
                var headerValue = httpContext.Request.Headers["X-Company-Id"].ToString();
                if (!string.IsNullOrEmpty(headerValue) && headerValue != "null")
                {
                    if (Guid.TryParse(headerValue, out var headerGuid)) return headerGuid;
                }

                // 2. Fallback to JWT Claim
                var claims = httpContext.User?.Claims;
                if (claims != null)
                {
                    var claimValue = claims.FirstOrDefault(c => 
                        c.Type.Equals("CompanyId", StringComparison.OrdinalIgnoreCase) || 
                        c.Type.Equals("companyid", StringComparison.OrdinalIgnoreCase))?.Value;

                    if (Guid.TryParse(claimValue, out var claimGuid)) return claimGuid;
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

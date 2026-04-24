using employeepayroll.Application.Common.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace employeepayroll.API.Services
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

                // 1. Try JWT Claim
                var claimValue = httpContext.User.Claims.FirstOrDefault(c => 
                    c.Type.Equals("CompanyId", StringComparison.OrdinalIgnoreCase) || 
                    c.Type.Equals("companyid", StringComparison.OrdinalIgnoreCase))?.Value;

                if (Guid.TryParse(claimValue, out var claimGuid)) return claimGuid;

                // 2. Fallback: Request Header (Important for Super Admin)
                var headerValue = httpContext.Request.Headers["X-Company-Id"].ToString();
                if (Guid.TryParse(headerValue, out var headerGuid)) return headerGuid;

                return null;
            }
        }

        public Guid? BranchId
        {
            get
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null) return null;

                // 1. Try JWT Claim
                var claimValue = httpContext.User.Claims.FirstOrDefault(c =>
                    c.Type.Equals("BranchId", StringComparison.OrdinalIgnoreCase) ||
                    c.Type.Equals("branchid", StringComparison.OrdinalIgnoreCase))?.Value;

                if (Guid.TryParse(claimValue, out var claimId)) return claimId;

                // 2. Fallback to Header (X-Branch-Id)
                var headerValue = httpContext.Request.Headers["X-Branch-Id"].ToString();
                if (Guid.TryParse(headerValue, out var headerId)) return headerId;

                return null;
            }
        }
    }
}

using Suppliers.Application.Common.Interfaces;
using System.Security.Claims;

namespace Suppliers.API.Services
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
                // 1. JWT Claim (Robust check)
                var claims = _httpContextAccessor.HttpContext?.User?.Claims;
                var claimValue = claims?.FirstOrDefault(c => 
                    c.Type.Equals("CompanyId", StringComparison.OrdinalIgnoreCase) || 
                    c.Type.Equals("companyid", StringComparison.OrdinalIgnoreCase))?.Value;

                if (Guid.TryParse(claimValue, out var claimGuid)) return claimGuid;

                // 2. Fallback: Request Header (Important for Super Admin)
                var headerValue = _httpContextAccessor.HttpContext?.Request.Headers["X-Company-Id"].ToString();
                if (Guid.TryParse(headerValue, out var headerGuid)) return headerGuid;

                return null;
            }
        }
    }
}

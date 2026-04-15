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
                // 1. JWT Claim
                var claimValue = _httpContextAccessor.HttpContext?.User?.FindFirstValue("CompanyId");
                if (Guid.TryParse(claimValue, out var claimGuid)) return claimGuid;

                // 2. Fallback: Request Header (Important for Super Admin)
                var headerValue = _httpContextAccessor.HttpContext?.Request.Headers["X-Company-Id"].ToString();
                if (Guid.TryParse(headerValue, out var headerGuid)) return headerGuid;

                return null;
            }
        }
    }
}

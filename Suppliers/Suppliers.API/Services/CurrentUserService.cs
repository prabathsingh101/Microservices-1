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
                var companyIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("CompanyId");
                return Guid.TryParse(companyIdClaim, out var guid) ? guid : null;
            }
        }
    }
}

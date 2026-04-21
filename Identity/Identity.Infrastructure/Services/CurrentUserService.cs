using System.Security.Claims;
using Identity.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Identity.Infrastructure.Services;

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
            var user = _httpContextAccessor.HttpContext?.User;
            var claim = user?.Claims.FirstOrDefault(c => c.Type == "CompanyId");
            return Guid.TryParse(claim?.Value, out var id) ? id : null;
        }
    }

    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var claim = user?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim?.Value, out var id) ? id : null;
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

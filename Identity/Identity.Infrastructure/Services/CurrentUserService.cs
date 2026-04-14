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
            var companyIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("CompanyId");
            if (Guid.TryParse(companyIdClaim, out var companyId))
            {
                return companyId;
            }
            return null;
        }
    }
}

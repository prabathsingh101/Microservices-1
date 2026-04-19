using System.Security.Claims;

namespace Inventory.API.Common;

public static class SecurityExtensions
{
    public static Guid GetCompanyId(this ClaimsPrincipal user)
    {
        var companyIdClaim = user.FindFirst("CompanyId")?.Value;
        if (Guid.TryParse(companyIdClaim, out var companyId))
        {
            return companyId;
        }

        // Check header as fallback if needed (though usually handled by CurrentUserService)
        return Guid.Empty;
    }
}

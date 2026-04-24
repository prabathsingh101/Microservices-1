using System.Security.Claims;

namespace Inventory.API.Common;

public static class SecurityExtensions
{
    public static Guid GetCompanyId(this ClaimsPrincipal user)
    {
        var companyIdClaim = user.FindFirst("CompanyId")?.Value;
        return Guid.TryParse(companyIdClaim, out var companyId) ? companyId : Guid.Empty;
    }

    public static string? GetBranchId(this ClaimsPrincipal user)
    {
        return user.FindFirst("BranchId")?.Value;
    }
}

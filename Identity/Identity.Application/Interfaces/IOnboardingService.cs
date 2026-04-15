using System;
using System.Threading.Tasks;

namespace Identity.Application.Interfaces;

public interface IOnboardingService
{
    /// <summary>
    /// Creates default roles and permissions for a new company/tenant.
    /// </summary>
    Task BootstrapCompanyAsync(Guid companyId, string companyName);
}

using System;
using System.Threading.Tasks;

namespace Identity.Application.Interfaces;

public interface IOnboardingService
{
    /// <summary>
    /// Creates default roles and permissions for a new company/tenant.
    /// </summary>
    Task BootstrapCompanyAsync(Guid companyId, string companyCode, string companyName, string? overrideAuthToken = null);
    
    /// <summary>
    /// Checks if a company name already exists in the Company microservice.
    /// </summary>
    Task<bool> IsCompanyNameDuplicateAsync(string name);
}

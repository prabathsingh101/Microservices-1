using Company.Application.Common.Models;
using Company.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Company.Application.Common.Interfaces
{
    public interface ICompanyRepository
    {
        Task<CompanyProfile?> GetCompanyProfileAsync();
        Task<CompanyProfile?> GetByIdAsync(Guid id);
        Task<CompanyProfile?> GetByNameAsync(string name);
        Task<bool> DeleteCompanyProfileAsync(Guid id);
        Task<Guid> InsertCompanyAsync(CompanyProfile company);
        Task<Guid> UpsertCompanyProfileAsync(CompanyProfile profile);
        Task<GridResponse<CompanyProfile>> GetPagedAsync(GridRequest request);
        Task<bool> HasDuplicateBankAccountAsync(string accountNumber, string ifscCode, Guid? excludeCompanyId);
    }
}


using Identity.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Identity.Application.Interfaces
{
    public interface ISubscriptionRepository
    {
        Task<Subscription?> GetByCompanyIdAsync(Guid companyId);
        Task<List<Subscription>> GetAllAsync();
        Task AddAsync(Subscription subscription);
        Task UpdateAsync(Subscription subscription);
    }
}

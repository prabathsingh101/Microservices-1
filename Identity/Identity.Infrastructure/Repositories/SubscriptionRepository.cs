using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Identity.Infrastructure.Repositories
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly IdentityDbContext _context;

        public SubscriptionRepository(IdentityDbContext context)
        {
            _context = context;
        }

        public async Task<Subscription?> GetByCompanyIdAsync(Guid companyId)
        {
            return await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.CompanyId == companyId);
        }

        public async Task<List<Subscription>> GetAllAsync()
        {
            return await _context.Subscriptions.ToListAsync();
        }

        public async Task AddAsync(Subscription subscription)
        {
            await _context.Subscriptions.AddAsync(subscription);
        }

        public async Task UpdateAsync(Subscription subscription)
        {
             _context.Subscriptions.Update(subscription);
             await Task.CompletedTask;
        }
    }
}

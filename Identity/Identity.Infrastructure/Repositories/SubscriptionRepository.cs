using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly IdentityDbContext _context;

        public SubscriptionRepository(IdentityDbContext context)
        {
            _context = context;
        }

        public async Task<Subscription?> GetByUserIdAsync(Guid userId)
        {
            return await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId);
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

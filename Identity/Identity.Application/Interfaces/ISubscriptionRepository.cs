using Identity.Domain.Entities;

namespace Identity.Application.Interfaces
{
    public interface ISubscriptionRepository
    {
        Task<Subscription?> GetByUserIdAsync(Guid userId);
        Task AddAsync(Subscription subscription);
        Task UpdateAsync(Subscription subscription);
    }
}

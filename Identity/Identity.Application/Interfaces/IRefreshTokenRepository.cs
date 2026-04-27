using Identity.Domain.Entities;

namespace Identity.Application.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetAsync(string token);
        Task AddAsync(RefreshToken token);
        Task RevokeAllAsync(Guid userId, string? revokedBy = null);
    }
}

using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly IdentityDbContext _context;

        public RefreshTokenRepository(IdentityDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetAsync(string token)
        {
            return await _context.RefreshTokens
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Token == token);
        }

        public async Task AddAsync(RefreshToken token)
        {
            await _context.RefreshTokens.AddAsync(token);
        }

        public async Task RevokeAllAsync(Guid userId, string? revokedBy = null)
        {
            await _context.RefreshTokens
                .Where(x => x.UserId == userId && !x.IsRevoked)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IsRevoked, true)
                    .SetProperty(x => x.LastModifiedBy, revokedBy)
                    .SetProperty(x => x.LastModifiedDate, DateTime.UtcNow));
        }
    }

}

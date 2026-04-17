using Identity.Application.Interfaces;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Identity.Domain.Users;
using Identity.Domain;

namespace Identity.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;

    public UserRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email);
    }

    public async Task<bool> ExistsByUserNameAsync(string userName)
    {
        return await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.UserName == userName);
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        // Login ke waqt bhi roles lagte hain, isliye include yahan bhi hona chahiye
        return await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    // ✅ FIXED: Is method mein roles aur tokens dono include kar diye hain
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetWithRolesByEmailAsync(string email)
    {
        // 🚀 UPDATED: Including RolePermissions and Menus for faster login logic
        return await _context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Menu)
            .Include(u => u.RefreshTokens)
            .AsSplitQuery() // Isse query fast ho jayegi aur timeout nahi aayega
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    // ✅ Already Correct: Ye method sahi tha, roles load kar raha tha
    public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
    {
        return await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.RefreshTokens)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u =>
                u.RefreshTokens.Any(rt => rt.Token == refreshToken));
    }

    public async Task<User?> GetByResetTokenAsync(string resetToken)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.ResetToken == resetToken);
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderBy(u => u.UserName)
            .ToListAsync();
    }

    public async Task<List<User>> GetByCompanyAsync(Guid companyId)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.CompanyId == companyId)
            .OrderBy(u => u.UserName)
            .ToListAsync();
    }

    public async Task UpdateAsync(User user)
    {
        // If the entity is not being tracked, attach it and follow standard update logic.
        if (_context.Entry(user).State == EntityState.Detached)
        {
            _context.Users.Update(user);
        }
        
        await _context.SaveChangesAsync();
    }
    public async Task DeleteAsync(Guid id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user != null)
        {
            _context.UserRoles.RemoveRange(user.UserRoles);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}
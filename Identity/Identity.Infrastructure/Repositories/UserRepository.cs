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

    public async Task<bool> ExistsByEmailAsync(string email, Guid? companyId)
    {
        return await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email && u.CompanyId == companyId);
    }

    public async Task<bool> ExistsByUserNameAsync(string userName, Guid? companyId)
    {
        return await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.UserName == userName && u.CompanyId == companyId);
    }

    // Overloads for more specific checks if needed
    public async Task<bool> ExistsByEmailAsync(string email, Guid? companyId, string? branchId)
    {
        var branchIds = string.IsNullOrEmpty(branchId) ? new List<string>() : branchId.Split(',').Select(b => b.Trim()).ToList();
        return await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email && u.CompanyId == companyId && (u.BranchId != null && branchIds.Any(b => ("," + u.BranchId + ",").Contains("," + b + ","))));
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task<User?> GetByEmailAsync(string email, Guid? companyId)
    {
        // Login ke waqt bhi roles lagte hain, isliye include yahan bhi hona chahiye
        return await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.CompanyId == companyId);
    }

    // ✅ FIXED: Is method mein roles aur tokens dono include kar diye hain
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetWithRolesByEmailAsync(string email, Guid? companyId)
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
            .FirstOrDefaultAsync(u => u.Email == email && u.CompanyId == companyId);
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

    public async Task<List<User>> GetByBranchAsync(Guid companyId, string branchId)
    {
        var branchIds = string.IsNullOrEmpty(branchId) ? new List<string>() : branchId.Split(',').Select(b => b.Trim()).ToList();
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.CompanyId == companyId && (u.BranchId != null && branchIds.Any(b => ("," + u.BranchId + ",").Contains("," + b + ","))))
            .OrderBy(u => u.UserName)
            .ToListAsync();
    }

    public async Task ClearRolesAsync(Guid userId)
    {
        var roles = await _context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
        if (roles.Any())
        {
            _context.UserRoles.RemoveRange(roles);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateAsync(User user)
    {
        try 
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // If the entity is detached or modified elsewhere, we try to handle it.
            var entry = _context.Entry(user);
            if (entry.State == EntityState.Detached)
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }
            else 
            {
                throw;
            }
        }
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
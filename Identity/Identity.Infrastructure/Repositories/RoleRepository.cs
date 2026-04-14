using Identity.Application.Interfaces;
using Identity.Domain.Roles;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly IdentityDbContext _context;

    public RoleRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByNameAsync(string roleName)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.RoleName == roleName);
    }

    public async Task<Role?> GetByIdAsync(Guid id)
    {
        return await _context.Roles.FindAsync(id);
    }

    public async Task<List<Role>> GetAllAsync()
    {
        return await _context.Roles.ToListAsync();
    }

    public async Task<List<Role>> GetByCompanyAsync(Guid companyId)
    {
        return await _context.Roles
            .Where(r => r.CompanyId == null || r.CompanyId == companyId)
            .ToListAsync();
    }

    public async Task AddAsync(Role role)
    {
        await _context.Roles.AddAsync(role);
    }

    public async Task UpdateAsync(Role role)
    {
        _context.Roles.Update(role);
        await Task.CompletedTask;
    }
}

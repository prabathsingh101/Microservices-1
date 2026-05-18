using Identity.Application.Interfaces;
using Identity.Domain.Menus;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Identity.Application.DTOs;

namespace Identity.Infrastructure.Repositories;

public class MenuRepository : IMenuRepository
{
    private readonly IdentityDbContext _context;

    public MenuRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Menu>> GetMenuByUserIdAsync(Guid userId)
    {
        // For now, returning all menus. Actual logic would filter based on RolePermissions.
        return await _context.Menus
            .Where(m => m.ParentId == null)
            .Include(m => m.Children)
            .OrderBy(m => m.Order)
            .ToListAsync();
    }

    public async Task<IEnumerable<Menu>> GetAllMenusAsync()
    {
        return await _context.Menus.ToListAsync();
    }

    public async Task<Menu?> GetByIdAsync(Guid id)
    {
        return await _context.Menus.FindAsync(id);
    }

    public async Task AddAsync(Menu menu)
    {
        await _context.Menus.AddAsync(menu);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Menu menu)
    {
        _context.Menus.Update(menu);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var menu = await GetByIdAsync(id);
        if (menu != null)
        {
            _context.Menus.Remove(menu);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateOrdersAsync(IEnumerable<MenuOrderUpdateDto> updates)
    {
        foreach (var update in updates)
        {
            var menu = await _context.Menus.FindAsync(update.Id);
            if (menu != null)
            {
                menu.Order = update.Order;
            }
        }
        await _context.SaveChangesAsync();
    }
}

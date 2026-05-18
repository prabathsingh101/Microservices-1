using Identity.Application.Interfaces;
using Identity.Domain.Menus;
using Identity.Application.DTOs;

namespace Identity.Application.Services;

public class MenuService : IMenuService
{
    private readonly IMenuRepository _menuRepository;

    public MenuService(IMenuRepository menuRepository)
    {
        _menuRepository = menuRepository;
    }

    public async Task<IEnumerable<Menu>> GetUserMenuAsync(Guid userId)
    {
        return await _menuRepository.GetMenuByUserIdAsync(userId);
    }

    public async Task<IEnumerable<Menu>> GetAllMenusAsync()
    {
        return await _menuRepository.GetAllMenusAsync();
    }

    public async Task CreateAsync(Menu menu)
    {
        await _menuRepository.AddAsync(menu);
    }

    public async Task UpdateAsync(Menu menu)
    {
        await _menuRepository.UpdateAsync(menu);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _menuRepository.DeleteAsync(id);
    }

    public async Task UpdateOrdersAsync(IEnumerable<MenuOrderUpdateDto> updates)
    {
        await _menuRepository.UpdateOrdersAsync(updates);
    }
}

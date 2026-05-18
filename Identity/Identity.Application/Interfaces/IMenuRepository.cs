using Identity.Domain.Menus;
using Identity.Application.DTOs;

namespace Identity.Application.Interfaces;

public interface IMenuRepository
{
    Task<IEnumerable<Menu>> GetMenuByUserIdAsync(Guid userId);
    Task<IEnumerable<Menu>> GetAllMenusAsync();
    Task<Menu?> GetByIdAsync(Guid id);
    Task AddAsync(Menu menu);
    Task UpdateAsync(Menu menu);
    Task DeleteAsync(Guid id);
    Task UpdateOrdersAsync(IEnumerable<MenuOrderUpdateDto> updates);
}

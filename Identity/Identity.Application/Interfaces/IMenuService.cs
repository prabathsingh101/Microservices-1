using Identity.Domain.Menus;
using Identity.Application.DTOs;

namespace Identity.Application.Interfaces;

public interface IMenuService
{
    Task<IEnumerable<Menu>> GetUserMenuAsync(Guid userId);
    Task<IEnumerable<Menu>> GetAllMenusAsync();
    Task CreateAsync(Menu menu);
    Task UpdateAsync(Menu menu);
    Task DeleteAsync(Guid id);
    Task UpdateOrdersAsync(IEnumerable<MenuOrderUpdateDto> updates);
}

using Identity.Domain.Menus;

namespace Identity.Application.Interfaces;

public interface IMenuService
{
    Task<IEnumerable<Menu>> GetUserMenuAsync(Guid userId);
    Task<IEnumerable<Menu>> GetAllMenusAsync();
    Task CreateAsync(Menu menu);
    Task UpdateAsync(Menu menu);
    Task DeleteAsync(Guid id);
}

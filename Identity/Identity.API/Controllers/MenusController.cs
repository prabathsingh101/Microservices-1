using Microsoft.AspNetCore.Mvc;
using Identity.Application.Interfaces;
using Identity.Domain.Menus;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenusController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenusController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet("user-menu")]
    public async Task<IActionResult> GetUserMenu()
    {
        // Actual logic would get current userId from Claims
        var userId = Guid.NewGuid(); // Placeholder
        var menus = await _menuService.GetUserMenuAsync(userId);
        return Ok(menus);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var menus = await _menuService.GetAllMenusAsync();
        return Ok(menus);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Menu menu)
    {
        await _menuService.CreateAsync(menu);
        return Ok(menu);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, Menu menu)
    {
        await _menuService.UpdateAsync(menu);
        return Ok(menu);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _menuService.DeleteAsync(id);
        return NoContent();
    }
}

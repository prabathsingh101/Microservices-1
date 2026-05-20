using Inventory.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Notification hamesha logged-in user ke liye honi chahiye
public class NotificationsController : ControllerBase
{
    private readonly INotificationRepository _repo;
    public NotificationsController(INotificationRepository repo) => _repo = repo;

    [HttpGet("unread")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
    public async Task<IActionResult> GetUnread() => Ok(await _repo.GetUnreadNotificationsAsync());

    [HttpGet("count")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
    public async Task<IActionResult> GetCount() => Ok(await _repo.GetUnreadCountAsync());

    [HttpPost("{id}/mark-read")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var result = await _repo.MarkAsReadAsync(id);
        return result ? Ok() : BadRequest("Notification not found");
    }

    [HttpPost("mark-all-read")]
    [Authorize(Roles = "Admin, User, Manager, Employee, Warehouse, Super Admin, Salesman")]
    public async Task<IActionResult> MarkAllRead() => Ok(await _repo.MarkAllAsReadAsync());
}

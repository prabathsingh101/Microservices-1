using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Interfaces;
using Identity.Domain.Users;

using MediatR;
using Identity.Application.Commands.EditUser;
using Identity.Domain;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IMediator _mediator;
    private readonly Identity.Infrastructure.Persistence.IdentityDbContext _context;

    public UsersController(IUserRepository userRepository, IMediator mediator, Identity.Infrastructure.Persistence.IdentityDbContext context)
    {
        _userRepository = userRepository;
        _mediator = mediator;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        
        // Permanent Fix: If user is any type of Admin, show ALL users across all companies
        bool isSuperAdmin = roles.Any(r => r.Contains("Admin", StringComparison.OrdinalIgnoreCase));

        IEnumerable<User> users;

        if (isSuperAdmin)
        {
            // Super Admin: See everything
            users = await _userRepository.GetAllUsersAsync();
        }
        else if (Guid.TryParse(companyIdClaim, out var companyId))
        {
            // Tenant Admin: Filter by their own company
            users = await _userRepository.GetByCompanyAsync(companyId);
        }
        else
        {
            // Fallback for anyone else
            users = await _userRepository.GetAllUsersAsync();
        }

        var allSubscriptions = await _context.Subscriptions.AsNoTracking().ToListAsync();

        // Project results to prevent circular references and hide sensitive data
        var result = users.Select(u => new
        {
            u.Id,
            u.UserName,
            u.Email,
            u.IsActive,
            u.CompanyId,
            CompanyName = u.CompanyId.HasValue 
                          ? (allSubscriptions.FirstOrDefault(s => s.CompanyId == u.CompanyId.Value)?.CompanyName ?? "Unknown") 
                          : "System Admin",
            Roles = u.UserRoles.Select(ur => ur.Role.RoleName).ToList(),
            RoleIds = u.UserRoles.Select(ur => ur.RoleId).ToList()
        });

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        var companyName = "System";
        if (user.CompanyId.HasValue)
        {
            var sub = await _context.Subscriptions.AsNoTracking().FirstOrDefaultAsync(s => s.CompanyId == user.CompanyId.Value);
            companyName = sub?.CompanyName ?? "Unknown";
        }

        return Ok(new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.IsActive,
            user.CompanyId,
            CompanyName = companyName,
            RoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList()
        });
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] bool isActive)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        user.SetActive(isActive);
        await _userRepository.UpdateAsync(user);
        return Ok();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] EditUserCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID in URL and body must match");

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("check-duplicate")]
    public async Task<IActionResult> CheckDuplicate([FromQuery] string? userName, [FromQuery] string? email)
    {
        if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(email))
            return BadRequest("Username or Email must be provided");

        bool exists = false;
        string message = "";

        if (!string.IsNullOrEmpty(userName) && await _userRepository.ExistsByUserNameAsync(userName))
        {
            exists = true;
            message = "Username already exists.";
        }
        else if (!string.IsNullOrEmpty(email) && await _userRepository.ExistsByEmailAsync(email))
        {
            exists = true;
            message = "Email already exists.";
        }

        return Ok(new { Exists = exists, Message = message });
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        await _userRepository.DeleteAsync(id);
        return Ok(new { Message = "User deleted successfully" });
    }
}

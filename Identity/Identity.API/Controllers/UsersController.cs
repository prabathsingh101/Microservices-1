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
        var branchIdClaim = User.FindFirst("BranchId")?.Value;
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();

        // 🚀 GLOBAL ADMIN BYPASS: Root Admin see EVERYTHING
        bool isGlobalRoot = roles.Contains("Default Admin");

        IEnumerable<User> users;

        if (isGlobalRoot)
        {
            // Root Admin: See all users from all companies
            users = await _userRepository.GetAllUsersAsync();
        }
        else if (Guid.TryParse(companyIdClaim, out var companyId))
        {
            // Tenant Admin: Filter by company and potentially branch
            var branchId = branchIdClaim;
            
            if (!string.IsNullOrEmpty(branchId) && !roles.Contains("Admin"))
            {
                // Branch User: Only see users in their own branch
                users = await _userRepository.GetByBranchAsync(companyId, branchId);
            }
            else
            {
                // Company Admin: See all users in their company
                users = await _userRepository.GetByCompanyAsync(companyId);
            }
        }
        else if (roles.Any(r => r.Contains("Admin", StringComparison.OrdinalIgnoreCase)))
        {
            // System-wide admin without a specific company claim
            users = await _userRepository.GetAllUsersAsync();
        }
        else 
        {
             // Fallback for anyone else
             return Ok(Enumerable.Empty<object>());
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
            u.BranchId,
            CompanyName = u.CompanyId.HasValue 
                          ? (allSubscriptions.FirstOrDefault(s => s.CompanyId == u.CompanyId.Value)?.CompanyName ?? "Unknown") 
                          : "System Admin",
            Roles = u.UserRoles.Select(ur => ur.Role.RoleName).ToList(),
            RoleIds = u.UserRoles.Select(ur => ur.RoleId).ToList()
        });

        return Ok(result);
    }

    [HttpPost("paged")]
    public async Task<IActionResult> GetPaged([FromBody] Identity.Application.Common.Models.GridRequest request)
    {
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        var branchIdClaim = User.FindFirst("BranchId")?.Value;
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        bool isGlobalRoot = roles.Contains("Default Admin");

        var query = _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsNoTracking();

        // 1. Tenant & Branch Filtering
        if (!isGlobalRoot && Guid.TryParse(companyIdClaim, out var companyId))
        {
            var branchId = branchIdClaim;
            
            if (!string.IsNullOrEmpty(branchId) && !roles.Contains("Admin"))
            {
                // Branch User: Only see users in their own branch
                query = query.Where(u => u.CompanyId == companyId && u.BranchId == branchId);
            }
            else
            {
                // Company Admin: See all users in their company
                query = query.Where(u => u.CompanyId == companyId);
            }
        }

        // 2. Search
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            
            // Join with Subscriptions to allow searching by CompanyName
            var companyIdsWithMatch = await _context.Subscriptions
                .Where(s => s.CompanyName.ToLower().Contains(term))
                .Select(s => s.CompanyId)
                .ToListAsync();

            query = query.Where(u => 
                u.UserName.ToLower().Contains(term) || 
                u.Email.ToLower().Contains(term) ||
                (u.CompanyId.HasValue && companyIdsWithMatch.Contains(u.CompanyId.Value)));
        }

        // Count for stats
        var totalCount = await query.CountAsync();
        var activeCount = await query.CountAsync(u => u.IsActive);
        var inactiveCount = totalCount - activeCount;

        // 3. Sorting
        if (!string.IsNullOrEmpty(request.SortColumn))
        {
            bool desc = request.SortOrder?.ToLower() == "desc";
            query = request.SortColumn.ToLower() switch
            {
                "username" => desc ? query.OrderByDescending(u => u.UserName) : query.OrderBy(u => u.UserName),
                "email" => desc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                _ => query.OrderBy(u => u.UserName)
            };
        }
        else
        {
            query = query.OrderBy(u => u.UserName);
        }

        // 4. Pagination
        var users = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var allSubscriptions = await _context.Subscriptions.AsNoTracking().ToListAsync();

        var resultItems = users.Select(u => new
        {
            u.Id,
            u.UserName,
            u.Email,
            u.IsActive,
            u.CompanyId,
            u.BranchId,
            CompanyName = u.CompanyId.HasValue
                          ? (allSubscriptions.FirstOrDefault(s => s.CompanyId == u.CompanyId.Value)?.CompanyName ?? "Unknown")
                          : "System Admin",
            Roles = u.UserRoles.Select(ur => ur.Role.RoleName).ToList(),
            RoleIds = u.UserRoles.Select(ur => ur.RoleId).ToList()
        });

        return Ok(new Identity.Application.Common.Models.GridResponse<object>
        {
            Items = resultItems.Cast<object>().ToList(),
            TotalCount = totalCount,
            ActiveCount = activeCount,
            InactiveCount = inactiveCount
        });
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
            user.BranchId,
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
    public async Task<IActionResult> CheckDuplicate([FromQuery] string? userName, [FromQuery] string? email, [FromQuery] Guid? companyId)
    {
        if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(email))
            return BadRequest("Username or Email must be provided");

        // Use provided companyId or fallback to claim
        if (!companyId.HasValue)
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            companyId = Guid.TryParse(companyIdClaim, out var cid) ? cid : null;
        }

        bool exists = false;
        string message = "";

        if (!string.IsNullOrEmpty(userName) && await _userRepository.ExistsByUserNameAsync(userName, companyId))
        {
            exists = true;
            message = "Username already exists in this company.";
        }
        else if (!string.IsNullOrEmpty(email) && await _userRepository.ExistsByEmailAsync(email, companyId))
        {
            exists = true;
            message = "Email already exists in this company.";
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

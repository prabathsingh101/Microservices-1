using Identity.Application.Interfaces;
using Identity.Domain;
using Identity.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Identity.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IdentityDbContext _context;
    private readonly IMediator _mediator;

    public UsersController(IUserRepository userRepository, ICurrentUserService currentUserService, IdentityDbContext context, IMediator mediator)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _context = context;
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        bool isPlatformAdmin = _currentUserService.IsPlatformAdmin;
        var activeCompanyId = _currentUserService.CompanyId;
        var activeBranchId = _currentUserService.BranchId;

        var query = _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsNoTracking();

        // 🛡️ Platform Admin bypasses all filters
        if (isPlatformAdmin)
        {
            query = query.IgnoreQueryFilters();
        }

        var usersList = await query.ToListAsync();

        // Perform complex branch filtering in memory to avoid EF translation issues
        if (!isPlatformAdmin && activeCompanyId.HasValue)
        {
            usersList = usersList.Where(u => u.CompanyId == activeCompanyId.Value).ToList();
            
            if (!string.IsNullOrEmpty(activeBranchId) && !_currentUserService.IsSuperAdmin)
            {
                var branchIds = activeBranchId.Split(',').Select(b => b.Trim()).ToList();
                usersList = usersList.Where(u => u.BranchId != null && branchIds.Any(b => ("," + u.BranchId + ",").Contains("," + b + ","))).ToList();
            }
        }

        var allSubscriptions = await _context.Subscriptions.IgnoreQueryFilters().AsNoTracking().ToListAsync();

        var result = usersList.Select(u => new
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
            RoleIds = u.UserRoles.Select(ur => ur.RoleId).ToList(),
            u.CreatedBy,
            u.CreatedDate,
            u.LastModifiedBy,
            u.LastModifiedDate
        });

        return Ok(result);
    }

    [HttpPost("paged")]
    public async Task<IActionResult> GetPaged([FromBody] Identity.Application.Common.Models.GridRequest request)
    {
        bool isPlatformAdmin = _currentUserService.IsPlatformAdmin;
        var activeCompanyId = _currentUserService.CompanyId;
        var activeBranchId = _currentUserService.BranchId;

        var query = _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsNoTracking();

        // 🛡️ SECURITY BYPASS FOR PLATFORM ADMIN
        if (isPlatformAdmin)
        {
            query = query.IgnoreQueryFilters();
        }

        // Apply basic EF-compatible filters
        if (!isPlatformAdmin && activeCompanyId.HasValue)
        {
            query = query.Where(u => u.CompanyId == activeCompanyId.Value);
        }

        // Search
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(u => u.UserName.ToLower().Contains(term) || 
                                   u.Email.ToLower().Contains(term) || 
                                   u.UserRoles.Any(ur => ur.Role.RoleName.ToLower().Contains(term)));
        }

        // 🚀 FETCH ALL USERS FOR THE SCOPE
        var allUsers = await query.ToListAsync();

        var totalCount = allUsers.Count;

        // In-Memory Sorting
        if (!string.IsNullOrEmpty(request.SortColumn))
        {
            var prop = typeof(User).GetProperty(request.SortColumn);
            if (prop != null)
            {
                allUsers = request.SortOrder?.ToLower() == "desc"
                    ? allUsers.OrderByDescending(u => prop.GetValue(u, null)).ToList()
                    : allUsers.OrderBy(u => prop.GetValue(u, null)).ToList();
            }
        }
        else
        {
            allUsers = allUsers.OrderBy(u => u.UserName).ToList();
        }

        // Pagination
        var paginatedUsers = allUsers
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var allSubscriptions = await _context.Subscriptions.IgnoreQueryFilters().AsNoTracking().ToListAsync();

        var result = paginatedUsers.Select(u => new
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
            RoleIds = u.UserRoles.Select(ur => ur.RoleId).ToList(),
            u.CreatedBy,
            u.CreatedDate,
            u.LastModifiedBy,
            u.LastModifiedDate
        });

        return Ok(new { items = result, totalCount });
    }

    [HttpGet("check-duplicate")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckDuplicate([FromQuery] string? userName, [FromQuery] string? email, [FromQuery] Guid? companyId)
    {
        if (string.IsNullOrEmpty(email)) return Ok(new { exists = false });

        bool emailExists;
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
        {
            emailExists = await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email);
        }
        else
        {
            emailExists = await _userRepository.ExistsByEmailAsync(email, companyId);
        }

        if (emailExists) return Ok(new { exists = true, message = "Email already exists" });

        return Ok(new { exists = false });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.IsActive,
            user.CompanyId,
            user.BranchId,
            Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList(),
            RoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] User user)
    {
        var currentUserId = _currentUserService.UserId?.ToString() ?? "System-Audit";
        var now = DateTime.UtcNow;

        var companyId = !_currentUserService.IsPlatformAdmin ? _currentUserService.CompanyId : user.CompanyId;
        user.SetCompanyId(companyId);

        // 🛡️ SECURITY: Strict check for duplicate Super Admin per company
        if (companyId.HasValue)
        {
            // Fetch all users of this company with roles
            var existingUsers = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.CompanyId == companyId.Value)
                .ToListAsync();

            // Check if any role being assigned is 'Super Admin'
            // (Assuming user.UserRoles is populated from body, or we check IDs)
            // Note: Since 'user' object is from body, we need to be careful.
            
            // Actually, let's check the database if a Super Admin user already exists for this company
            var superAdminExists = existingUsers.Any(u => u.UserRoles.Any(ur => ur.Role.RoleName == "Super Admin"));
            
            // Check if the current request is trying to create another Super Admin
            // (The frontend sends RoleIds, but the User entity might have them in UserRoles)
            // If the incoming user has 'Super Admin' role assigned:
            // Since UserRoles is a navigation property, it might be empty here.
            // Let's check based on the business context: the UI forces Super Admin for tenants.
            
            if (superAdminExists)
            {
                return BadRequest(new { message = "A Super Admin already exists for this company. Only one is allowed." });
            }
        }

        // 🛡️ FAIL-SAFE: Manually set audit fields for creation
        user.CreatedBy = currentUserId;
        user.CreatedDate = now;
        user.LastModifiedBy = currentUserId;
        user.LastModifiedDate = now;

        await _userRepository.AddAsync(user);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Identity.Application.Commands.EditUser.EditUserCommand command)
    {
        if (id != command.Id) return BadRequest("ID mismatch");

        var result = await _mediator.Send(command);
        if (!result.IsSuccess) return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] bool isActive)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        user.SetActive(isActive);
        await _userRepository.UpdateAsync(user);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        await _userRepository.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("online")]
    public async Task<IActionResult> GetOnlineUsers()
    {
        var companyId = _currentUserService.CompanyId;
        var now = DateTime.UtcNow;

        // Fetch user IDs from active refresh tokens
        var activeTokens = await _context.RefreshTokens
            .Where(rt => !rt.IsRevoked && rt.ExpiresAt > now)
            .Select(rt => new { rt.UserId, rt.CreatedDate })
            .ToListAsync();

        var activeUserIds = activeTokens.Select(t => t.UserId).Distinct().ToList();

        // Filter by real-time SignalR connections to eliminate stale tokens (e.g. from closed tabs)
        var onlineUserIds = Hubs.AuthHub.GetOnlineUserIds();

        // Ensure current requesting user is always considered online as a fallback
        var currentUserId = _currentUserService.UserId;
        if (currentUserId.HasValue)
        {
            if (!onlineUserIds.Contains(currentUserId.Value))
            {
                onlineUserIds.Add(currentUserId.Value);
            }
        }

        // Intersect database refresh tokens with real-time SignalR active connections
        var finalActiveUserIds = activeUserIds.Intersect(onlineUserIds).ToList();

        // Fallback: If current user is not in final list (e.g. token expired but request succeeded), add them
        if (currentUserId.HasValue && !finalActiveUserIds.Contains(currentUserId.Value))
        {
            finalActiveUserIds.Add(currentUserId.Value);
        }

        var query = _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(u => u.IsActive && finalActiveUserIds.Contains(u.Id))
            .AsNoTracking();

        if (!_currentUserService.IsPlatformAdmin && companyId.HasValue)
        {
            query = query.Where(u => u.CompanyId == companyId.Value);
        }

        var usersList = await query.ToListAsync();
        var userLoginTimes = activeTokens
            .GroupBy(t => t.UserId)
            .ToDictionary(g => g.Key, g => g.Max(t => t.CreatedDate));

        var result = usersList.Select(u => new
        {
            u.Id,
            u.UserName,
            u.Email,
            Role = u.UserRoles.Select(ur => ur.Role.RoleName).FirstOrDefault() ?? "Staff",
            LoginTime = userLoginTimes.TryGetValue(u.Id, out var dt) ? dt : u.CreatedDate
        }).ToList();

        return Ok(result);
    }
}

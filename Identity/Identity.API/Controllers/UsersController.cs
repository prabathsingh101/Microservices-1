using Microsoft.AspNetCore.Mvc;
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

    public UsersController(IUserRepository userRepository, IMediator mediator)
    {
        _userRepository = userRepository;
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        IEnumerable<User> users;

        if (Guid.TryParse(companyIdClaim, out var companyId))
        {
            // Tenant Admin: Filter by their own company
            users = await _userRepository.GetByCompanyAsync(companyId);
        }
        else
        {
            // Super Admin: See everything
            users = await _userRepository.GetAllUsersAsync();
        }

        // Project results to prevent circular references and hide sensitive data
        var result = users.Select(u => new
        {
            u.Id,
            u.UserName,
            u.Email,
            u.IsActive,
            u.CompanyId,
            Roles = u.UserRoles.Select(ur => ur.Role.RoleName).ToList()
        });

        return Ok(result);
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
}

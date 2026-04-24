using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Commands.RegisterUser;

public record RegisterUserCommand(
    string UserName,
    string Email,
    string Password,
    List<Guid> RoleIds,
    Guid? CompanyId = null,
    string? BranchId = null,
    string? CompanyName = null
) : IRequest<Guid>;

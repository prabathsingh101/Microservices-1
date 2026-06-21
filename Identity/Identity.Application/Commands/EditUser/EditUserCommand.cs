using Identity.Application.Common;
using MediatR;

namespace Identity.Application.Commands.EditUser;

public record EditUserCommand(
    Guid Id,
    string UserName,
    string Email,
    string? Password,
    bool IsActive,
    List<Guid> RoleIds,
    Guid? CompanyId = null,
    string? BranchId = null,
    string? ProfileImage = null,
    string? FirstName = null,
    string? LastName = null,
    string? PhoneNumber = null,
    string? Designation = null,
    string? Department = null,
    string? Address = null,
    string? City = null,
    string? State = null,
    string? Pincode = null,
    string? Gender = null,
    DateTime? DateOfBirth = null,
    string? AadhaarUrl = null,
    string? PanCardUrl = null
) : IRequest<Result<Guid>>;

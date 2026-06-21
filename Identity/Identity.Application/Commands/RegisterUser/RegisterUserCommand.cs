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
    string? CompanyName = null,
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
) : IRequest<Guid>;

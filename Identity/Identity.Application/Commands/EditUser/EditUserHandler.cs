using Identity.Application.Common;
using Identity.Application.Interfaces;
using Identity.Domain;
using Identity.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Identity.Application.Commands.EditUser;

public class EditUserHandler : IRequestHandler<EditUserCommand, Result<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository; // Assuming this exists or needed for validation
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public EditUserHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher<User> passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(EditUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id);
        if (user == null)
            return Result<Guid>.Failure("User not found");

        // Check for duplicates within the same company
        if (user.UserName != request.UserName && await _userRepository.ExistsByUserNameAsync(request.UserName, user.CompanyId))
            return Result<Guid>.Failure("Username already exists in this company context");

        if (user.Email != request.Email && await _userRepository.ExistsByEmailAsync(request.Email, user.CompanyId))
            return Result<Guid>.Failure("Email already exists in this company context");

        // Update details
        user.UpdateDetails(request.UserName, request.Email, request.IsActive);

        // Update CompanyId/BranchId if provided
        if (request.CompanyId != user.CompanyId)
        {
             user.SetCompanyId(request.CompanyId);
        }

        if (request.BranchId != user.BranchId)
        {
            user.SetBranchId(request.BranchId);
        }

        // Update Roles
        if (request.RoleIds != null)
        {
            user.UpdateRoles(request.RoleIds);
        }

        // Update Password if provided (Optional during edit)
        if (!string.IsNullOrEmpty(request.Password))
        {
             var hash = _passwordHasher.HashPassword(user, request.Password);
             user.SetPasswordHash(hash);
        }

        await _userRepository.UpdateAsync(user);

        return Result<Guid>.Success(user.Id);
    }
}

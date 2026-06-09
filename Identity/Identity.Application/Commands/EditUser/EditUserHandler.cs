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
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public EditUserHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher<User> passwordHasher,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Guid>> Handle(EditUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id);
        if (user == null)
            return Result<Guid>.Failure("User not found");

        // Update details (Email ID is readonly during edit)
        user.UpdateDetails(request.UserName, user.Email, request.IsActive);

        // Update CompanyId/BranchId if provided
        if (request.CompanyId != user.CompanyId)
        {
             user.SetCompanyId(request.CompanyId);
        }

        // Handle BranchId update (explicitly allow null for Global)
        if (request.BranchId != user.BranchId)
        {
            var targetBid = string.IsNullOrEmpty(request.BranchId) ? null : request.BranchId;
            user.SetBranchId(targetBid);
        }

        // Update Roles
        if (request.RoleIds != null)
        {
            // 1. Detach tracked UserRole entities to prevent EF Core from trying to update/delete them
            await _userRepository.DetachUserRolesAsync(user.Id);

            // 2. Clear roles from database directly
            await _userRepository.ClearRolesAsync(user.Id);

            // 3. Clear in-memory tracked collection
            user.ClearRolesCollection();

            // 4. Assign new roles as fresh entries (forcing INSERT statements as Id is Guid.Empty)
            foreach (var roleId in request.RoleIds)
            {
                user.AssignRole(roleId);
            }
        }

        // Update Password if provided (Optional during edit)
        if (!string.IsNullOrEmpty(request.Password))
        {
             var hash = _passwordHasher.HashPassword(user, request.Password);
             user.SetPasswordHash(hash);
        }

        // 🛡️ FAIL-SAFE: Manually set audit fields before saving
        user.LastModifiedBy = _currentUserService.UserId?.ToString() ?? "System-Manual";
        user.LastModifiedDate = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        return Result<Guid>.Success(user.Id);
    }
}

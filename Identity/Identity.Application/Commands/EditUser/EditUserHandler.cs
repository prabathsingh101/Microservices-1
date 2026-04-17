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

        // Check for duplicates
        if (user.UserName != request.UserName && await _userRepository.ExistsByUserNameAsync(request.UserName))
            return Result<Guid>.Failure("Username already exists");

        if (user.Email != request.Email && await _userRepository.ExistsByEmailAsync(request.Email))
            return Result<Guid>.Failure("Email already exists");

        // Update details
        user.UpdateDetails(request.UserName, request.Email, request.IsActive);

        // Update CompanyId if provided
        if (request.CompanyId != user.CompanyId)
        {
             user.SetCompanyId(request.CompanyId);
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
        
        // Save changes via UnitOfWork if pattern dictates, usually Repo.UpdateAsync does not save.
        // But some repos do save. Based on RegisterUserHandler, it calls _uow.SaveChangesAsync().
        // Wait, check RegisterUserHandler line 58: await _uow.SaveChangesAsync(cancellationToken);
        
        // Assuming _userRepository.UpdateAsync just tracks changes.
        // We probably need to call SaveChangesAsync on UoW.
        // But let's check RegisterUserHandler again.
        // It calls _users.AddAsync(user) then _uow.SaveChangesAsync().
        
        // So for Update:
        // _users.UpdateAsync(user) - if needed to attach/modify state
        // then _uow.SaveChangesAsync()
        
        // However, standard EF Core tracking might handle it if retrieved from context.
        // But usually explicit Update call in repo is good pattern.
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(user.Id);
    }
}

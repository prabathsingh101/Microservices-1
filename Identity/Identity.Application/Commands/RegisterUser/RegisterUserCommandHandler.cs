
using Identity.Application.Commands.RegisterUser;
using Identity.Application.Interfaces;
using Identity.Domain.Users;
using Identity.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Identity.Domain;

public class RegisterUserHandler
    : IRequestHandler<RegisterUserCommand, Guid>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IUnitOfWork _uow;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly ICurrentUserService _currentUserService;

    public RegisterUserHandler(
        IUserRepository users,
        IRoleRepository roles,
        IPasswordHasher<User> passwordHasher,
        IUnitOfWork uow,
        ISubscriptionRepository subscriptions,
        ICurrentUserService currentUserService)
    {
        _users = users;
        _roles = roles;
        _passwordHasher = passwordHasher;
        _uow = uow;
        _subscriptions = subscriptions;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        // ✅ Step 1: Resolve Target Company & Branch ID
        Guid? targetCompanyId = null;
        string? targetBranchId = null;

        // Priority 1: Explicitly provided CompanyId/BranchId in the request
        if (request.CompanyId.HasValue && request.CompanyId.Value != Guid.Empty)
        {
            targetCompanyId = request.CompanyId.Value;
        }
        else if (_currentUserService.CompanyId.HasValue && _currentUserService.CompanyId.Value != Guid.Empty)
        {
            targetCompanyId = _currentUserService.CompanyId.Value;
        }

        if (!string.IsNullOrEmpty(request.BranchId))
        {
            targetBranchId = request.BranchId;
        }
        else if (!string.IsNullOrEmpty(_currentUserService.BranchId))
        {
            targetBranchId = _currentUserService.BranchId;
        }

        // 🚀 SAFETY: If Role is provided, double check the CompanyId from the Role 
        // to ensure cross-tenant user creation doesn't happen accidentally
        if (request.RoleIds != null && request.RoleIds.Any() && !targetCompanyId.HasValue)
        {
            var firstRole = await _roles.GetByIdAsync(request.RoleIds.First());
            if (firstRole != null && firstRole.CompanyId.HasValue)
            {
                targetCompanyId = firstRole.CompanyId;
            }
        }

        // ✅ Step 2: Tenant-aware Duplicate Check
        if (await _users.ExistsByEmailAsync(request.Email, targetCompanyId))
            throw new InvalidOperationException("Email already exists in this company context");

        if (await _users.ExistsByUserNameAsync(request.UserName, targetCompanyId))
            throw new InvalidOperationException("Username already exists in this company context");

        var user = new User(request.UserName, request.Email);
        if (targetCompanyId.HasValue) user.SetCompanyId(targetCompanyId.Value);
        if (!string.IsNullOrEmpty(targetBranchId)) user.SetBranchId(targetBranchId);

        // ✅ Step 2: Hash Password
        var hash = _passwordHasher.HashPassword(user, request.Password);
        user.SetPasswordHash(hash);

        // ✅ Step 3: Assign Roles (Now they will have the correct CompanyId)
        if (request.RoleIds != null && request.RoleIds.Any())
        {
            foreach (var roleId in request.RoleIds)
            {
                var role = await _roles.GetByIdAsync(roleId);
                if (role == null) throw new InvalidOperationException($"Invalid Role ID: {roleId}");
                user.AssignRole(role.Id);
            }
        }

        await _users.AddAsync(user);
        await _uow.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}

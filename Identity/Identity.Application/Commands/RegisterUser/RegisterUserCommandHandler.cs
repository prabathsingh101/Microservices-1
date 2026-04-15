
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
        if (await _users.ExistsByEmailAsync(request.Email))
            throw new InvalidOperationException("Email already exists");

        var user = new User(request.UserName, request.Email);

        // ✅ HASH PASSWORD HERE
        var hash = _passwordHasher.HashPassword(user, request.Password);
        user.SetPasswordHash(hash);

        // Assign Multiple Roles
        if (request.RoleIds != null && request.RoleIds.Any())
        {
            foreach (var roleId in request.RoleIds)
            {
                var role = await _roles.GetByIdAsync(roleId);
                if (role == null) throw new InvalidOperationException($"Invalid Role ID: {roleId}");
                user.AssignRole(role.Id);
            }
        }
        else
        {
             // Default Role assignment if none provided? Or enforce roles?
             // Maybe assign "User" role by default if name based lookup was used before.
             // But now we rely on explicit IDs. Let's assume validation handles requirement.
        }

        await _users.AddAsync(user);

        // --- Multi-tenant Handling ---
        // 1. Check if an Admin is creating this user (from Token)
        if (_currentUserService.CompanyId.HasValue)
        {
            user.SetCompanyId(_currentUserService.CompanyId.Value);
        }
        // 2. Or if explicitly provided in request (e.g. from System Admin portal)
        else if (request.CompanyId.HasValue)
        {
            user.SetCompanyId(request.CompanyId.Value);
        }
        // 3. Otherwise, it's a new public signup - leave CompanyId NULL for now
        // User will call 'setup-company' after trial activation.

        await _uow.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}

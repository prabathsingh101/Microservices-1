
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

        // ✅ Step 1: Set CompanyId FIRST (so roles can inherit it)
        if (_currentUserService.CompanyId.HasValue)
        {
            user.SetCompanyId(_currentUserService.CompanyId.Value);
        }
        else if (request.CompanyId.HasValue)
        {
            user.SetCompanyId(request.CompanyId.Value);
        }

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

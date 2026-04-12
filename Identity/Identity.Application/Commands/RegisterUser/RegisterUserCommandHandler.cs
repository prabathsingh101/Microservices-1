
using Identity.Application.Commands.RegisterUser;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

public class RegisterUserHandler
    : IRequestHandler<RegisterUserCommand, Guid>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IUnitOfWork _uow;
    private readonly ISubscriptionRepository _subscriptions;

    public RegisterUserHandler(
        IUserRepository users,
        IRoleRepository roles,
        IPasswordHasher<User> passwordHasher,
        IUnitOfWork uow,
        ISubscriptionRepository subscriptions)
    {
        _users = users;
        _roles = roles;
        _passwordHasher = passwordHasher;
        _uow = uow;
        _subscriptions = subscriptions;
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

        // --- Create Default 7-day Trial ---
        var trial = new Subscription(user.Id, "Trial", 7);
        await _subscriptions.AddAsync(trial);

        await _uow.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}

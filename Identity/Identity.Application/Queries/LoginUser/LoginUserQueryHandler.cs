using Identity.Application.Common;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Application.Queries.LoginUser;
using Identity.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

public class LoginUserQueryHandler
    : IRequestHandler<LoginUserQuery, Result<AuthResponse>>
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _tokens;
    private readonly IPasswordHasher<User> _hasher;
    private readonly IJwtService _jwt;
    private readonly IUnitOfWork _uow;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IRolePermissionRepository _permissionRepository;

    public LoginUserQueryHandler(
        IUserRepository users,
        IRefreshTokenRepository tokens,
        IPasswordHasher<User> hasher,
        IJwtService jwt,
        IUnitOfWork uow,
        ISubscriptionRepository subscriptions,
        IRolePermissionRepository permissionRepository)
    {
        _users = users;
        _tokens = tokens;
        _hasher = hasher;
        _jwt = jwt;
        _uow = uow;
        _subscriptions = subscriptions;
        _permissionRepository = permissionRepository;
    }

    public async Task<Result<AuthResponse>> Handle(
    LoginUserQuery request,
    CancellationToken ct)
    {
        // 1. Fetch user with roles
        var user = await _users.GetWithRolesByEmailAsync(request.Dto.Email);
        if (user == null)
            return Result<AuthResponse>.Failure("Invalid credentials");

        // 2. Verify Password
        var verify = _hasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Dto.Password);

        if (verify == PasswordVerificationResult.Failed)
            return Result<AuthResponse>.Failure("Invalid credentials");

        // 3. Company Subscription Check
        string? companyName = null;
        bool isExpired = false;
        string subStatus = "Active";

        if (user.CompanyId.HasValue)
        {
            var subscription = await _subscriptions.GetByCompanyIdAsync(user.CompanyId.Value);
            if (subscription != null)
            {
                companyName = subscription.CompanyName;
                if (!subscription.IsActive || DateTime.UtcNow > subscription.EndDate)
                {
                    isExpired = true;
                    subStatus = "Expired";
                }
                else
                {
                    subStatus = subscription.PlanType;
                }
            }
            else 
            {
                subStatus = "No Subscription";
            }
        }

        // 4. Fetch Permissions for all User Roles
        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var aggregatedPermissions = await _permissionRepository.GetAggregatedPermissionsAsync(roleIds);

        // 5. Revoke old tokens
        await _tokens.RevokeAllAsync(user.Id);

        var rolesStrings = user.UserRoles
            .Select(r => r.Role?.RoleName ?? "User")
            .ToList();

        // 6. Generate JWT
        var auth = _jwt.Generate(user, rolesStrings, companyName);

        // 7. Additional mapping
        auth.IsSubscriptionExpired = isExpired;
        auth.SubscriptionStatus = subStatus;
        auth.Permissions = aggregatedPermissions.ToList();

        // 8. Add Refresh Token
        await _tokens.AddAsync(
            new RefreshToken(
                user.Id,
                auth.RefreshToken,
                auth.ExpiresAt.AddDays(7)));

        await _uow.SaveChangesAsync(ct);

        return Result<AuthResponse>.Success(auth);
    }
}

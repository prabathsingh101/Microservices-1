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

    public LoginUserQueryHandler(
        IUserRepository users,
        IRefreshTokenRepository tokens,
        IPasswordHasher<User> hasher,
        IJwtService jwt,
        IUnitOfWork uow,
        ISubscriptionRepository subscriptions)
    {
        _users = users;
        _tokens = tokens;
        _hasher = hasher;
        _jwt = jwt;
        _uow = uow;
        _subscriptions = subscriptions;
    }

    public async Task<Result<AuthResponse>> Handle(
    LoginUserQuery request,
    CancellationToken ct)
    {
        var user = await _users.GetWithRolesByEmailAsync(request.Dto.Email);
        if (user == null)
            return Result<AuthResponse>.Failure("Invalid credentials");

        var verify = _hasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Dto.Password);

        if (verify == PasswordVerificationResult.Failed)
            return Result<AuthResponse>.Failure("Invalid credentials");

        // --- Company Subscription Check ---
        bool isExpired = false;
        string subStatus = "Active";

        if (user.CompanyId.HasValue)
        {
            var subscription = await _subscriptions.GetByCompanyIdAsync(user.CompanyId.Value);
            if (subscription != null)
            {
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
                // No subscription record found for company -> Treat as no access or default trial?
                // For now, let's assume active if missing to avoid locking out existing data during transition
                subStatus = "No Subscription";
            }
        }

        // ✅ SAFE: bulk revoke
        await _tokens.RevokeAllAsync(user.Id);

        var roles = user.UserRoles
            .Select(r => r.Role.RoleName)
            .ToList();

        // 1. Generate Auth object (Yahan check karein ki Generate method ID set karta hai ya nahi)
        var auth = _jwt.Generate(user, roles);

        // 2. Explicitly mapping UserId (AGAR auth.UserId zero/empty aa raha hai toh ye line zaroori hai)
        auth.UserId = user.Id;
        auth.IsSubscriptionExpired = isExpired;
        auth.SubscriptionStatus = subStatus;

        await _tokens.AddAsync(
            new RefreshToken(
                user.Id,
                auth.RefreshToken,
                auth.ExpiresAt.AddDays(7)));

        await _uow.SaveChangesAsync(ct);

        // 3. Return the fully mapped response
        return Result<AuthResponse>.Success(auth);
    }
}

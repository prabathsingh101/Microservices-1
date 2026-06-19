using Identity.Application.Commands.SocialLogin;
using Identity.Application.Common;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain;
using Identity.Domain.Entities;
using Identity.Domain.Users;
using MediatR;

// Alias to avoid conflict with Identity.Application.Commands.RefreshToken namespace
using DomainRefreshToken = Identity.Domain.Entities.RefreshToken;
using Identity.Domain.Permissions;

namespace Identity.Application.Commands.SocialLogin;

public class SocialLoginCommandHandler : IRequestHandler<SocialLoginCommand, Result<AuthResponse>>
{
    private readonly IGoogleTokenVerifier _googleVerifier;
    private readonly IUserRepository _users;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IRoleRepository _roles;
    private readonly IRolePermissionRepository _permissions;
    private readonly IRefreshTokenRepository _tokens;
    private readonly IJwtService _jwt;
    private readonly IUnitOfWork _uow;
    private readonly IOnboardingService _onboarding;
    private readonly ISignalRNotificationService _signalR;

    public SocialLoginCommandHandler(
        IGoogleTokenVerifier googleVerifier,
        IUserRepository users,
        ISubscriptionRepository subscriptions,
        IRoleRepository roles,
        IRolePermissionRepository permissions,
        IRefreshTokenRepository tokens,
        IJwtService jwt,
        IUnitOfWork uow,
        IOnboardingService onboarding,
        ISignalRNotificationService signalR)
    {
        _googleVerifier = googleVerifier;
        _users = users;
        _subscriptions = subscriptions;
        _roles = roles;
        _permissions = permissions;
        _tokens = tokens;
        _jwt = jwt;
        _uow = uow;
        _onboarding = onboarding;
        _signalR = signalR;
    }

    public async Task<Result<AuthResponse>> Handle(SocialLoginCommand request, CancellationToken ct)
    {
        Console.WriteLine("[SOCIAL-LOGIN] Starting Google social login flow...");

        // ─────────────────────────────────────────────
        // 1. Google Token Verify karo
        // ─────────────────────────────────────────────
        var googleUser = await _googleVerifier.VerifyAsync(request.IdToken);
        if (googleUser == null)
        {
            Console.WriteLine("[SOCIAL-LOGIN] Google token verification failed.");
            return Result<AuthResponse>.Failure("Invalid Google token. Please try again.");
        }

        Console.WriteLine($"[SOCIAL-LOGIN] Google verified. Email: {googleUser.Email}, Name: {googleUser.Name}");

        // ─────────────────────────────────────────────
        // 2. Check: Kya yeh email pehle se exist karta hai?
        //    (companyId = null → global check for social users)
        // ─────────────────────────────────────────────
        var existingUser = await _users.GetGlobalUserWithRolesByEmailAsync(googleUser.Email);

        if (existingUser != null)
        {
            Console.WriteLine($"[SOCIAL-LOGIN] Existing user found. ID: {existingUser.Id}. Logging in directly.");

            // ── Existing user → Sirf login karo, koi nayi entry nahi ──
            return await GenerateLoginResponse(existingUser, ct);
        }

        // ─────────────────────────────────────────────
        // 3. NEW USER → 5 Tables mein entry karo
        // ─────────────────────────────────────────────
        Console.WriteLine("[SOCIAL-LOGIN] New user. Starting onboarding into 5 tables...");

        // Step 3a: CompanyCode generate karo (unique)
        var companyCode = await GenerateUniqueCompanyCodeAsync(googleUser.Email);
        Console.WriteLine($"[SOCIAL-LOGIN] Generated CompanyCode: {companyCode}");

        // Step 3b: CompanyName generate karo (unique)
        var companyName = await GenerateUniqueCompanyNameAsync(googleUser.Name);
        Console.WriteLine($"[SOCIAL-LOGIN] Generated CompanyName: {companyName}");

        // Step 3c: New CompanyId
        var companyId = Guid.NewGuid();

        // Step 3e: TABLE 1 → Subscriptions
        var subscription = new Subscription(
            companyId: companyId,
            companyCode: companyCode,
            companyName: companyName,
            planType: "Trial",
            durationDays: 30,
            companyTagline: "Welcome to " + companyName
        );
        await _subscriptions.AddAsync(subscription);
        Console.WriteLine($"[SOCIAL-LOGIN] Subscription created. CompanyCode: {companyCode}");

        // Generate a temporary JWT token to authorize the internal CompanyProfile API call
        var tempUser = new User(googleUser.Name, googleUser.Email);
        tempUser.SetCompanyId(companyId);
        var tempAuth = _jwt.Generate(tempUser, new List<string> { "Admin" }, companyName);

        // Step 3f: TABLES 3, 4, 5 → Roles + RolePermissions + CompanyProfiles
        // OnboardingService handles: Roles, RolePermissions, CompanyProfiles (cross-service call)
        await _onboarding.BootstrapCompanyAsync(companyId, companyCode, companyName, tempAuth.AccessToken);
        Console.WriteLine($"[SOCIAL-LOGIN] Bootstrap done. Roles + RolePermissions + CompanyProfile created.");

        // Fetch the newly created Admin role
        var adminRole = (await _roles.GetByCompanyAsync(companyId))
            .FirstOrDefault(r => r.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase));

        // Step 3g: TABLE 2 → Users
        var newUser = new User(googleUser.Name, googleUser.Email);
        newUser.SetCompanyId(companyId);
        newUser.SetAuthProvider("google");
        newUser.SetGoogleId(googleUser.GoogleId);
        // Note: PasswordHash intentionally NOT set (null) for social users
        
        if (adminRole != null)
        {
            newUser.AssignRole(adminRole.Id);
            Console.WriteLine($"[SOCIAL-LOGIN] Admin role assigned to user.");
        }
        else
        {
            Console.WriteLine("[SOCIAL-LOGIN] WARNING: Admin role not found after bootstrap!");
        }

        await _users.AddAsync(newUser);
        Console.WriteLine($"[SOCIAL-LOGIN] User created. ID: {newUser.Id}");

        // Save all changes (Subscription, User, UserRoles)
        await _uow.SaveChangesAsync(ct);

        // ─────────────────────────────────────────────
        // 4. Login Response generate karo
        // ─────────────────────────────────────────────
        // Reload with full roles + permissions
        var finalUser = await _users.GetWithRolesByEmailAsync(googleUser.Email, companyId);
        if (finalUser == null)
        {
            return Result<AuthResponse>.Failure("Social login setup failed. Please try again.");
        }

        Console.WriteLine("[SOCIAL-LOGIN] All done. Generating JWT...");
        return await GenerateLoginResponse(finalUser, ct);
    }

    // ─────────────────────────────────────────────────────────────
    // Helper: JWT + Refresh Token generate karo (same as normal login)
    // ─────────────────────────────────────────────────────────────
    private async Task<Result<AuthResponse>> GenerateLoginResponse(User user, CancellationToken ct)
    {
        // Subscription info
        string? companyName = null;
        string? companyTagline = null;
        string? companyCode = null;
        bool isExpired = false;
        string subStatus = "Active";

        if (user.CompanyId.HasValue)
        {
            var sub = await _subscriptions.GetByCompanyIdAsync(user.CompanyId.Value);
            if (sub != null)
            {
                companyName = sub.CompanyName;
                companyTagline = sub.CompanyTagline ?? sub.CompanyName;
                companyCode = sub.CompanyCode;
                if (!sub.IsActive || DateTime.UtcNow > sub.EndDate)
                {
                    isExpired = true;
                    subStatus = "Expired";
                }
                else
                {
                    subStatus = sub.PlanType;
                }
            }
        }

        // Permissions
        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var aggregatedPermissions = await _permissions.GetAggregatedPermissionsAsync(roleIds, user.Id, user.BranchId);

        // Revoke old refresh tokens
        await _tokens.RevokeAllAsync(user.Id, user.Email);

        var rolesStrings = user.UserRoles
            .Select(r => r.Role?.RoleName ?? "User")
            .ToList();

        // ** Concurrent Login Prevention **
        // NOTE: user may be untracked, so update session ID directly in DB
        var sessionId = Guid.NewGuid().ToString();
        await _users.UpdateSessionIdAsync(user.Id, sessionId);
        user.SetCurrentSessionId(sessionId);

        // Generate JWT
        var auth = _jwt.Generate(user, rolesStrings, companyName);
        auth.CompanyTagline = companyTagline;
        auth.CompanyCode = companyCode;
        auth.IsSubscriptionExpired = isExpired;
        auth.SubscriptionStatus = subStatus;
        auth.Permissions = aggregatedPermissions.ToList();

        // Add new Refresh Token
        var refreshToken = new DomainRefreshToken(user.Id, auth.RefreshToken, DateTime.UtcNow.AddDays(7), user.CompanyId, user.BranchId)
        {
            CreatedBy = user.Email,
            CreatedDate = DateTime.UtcNow
        };
        await _tokens.AddAsync(refreshToken);

        // Notify old sessions to logout via SignalR
        await _signalR.SendForceLogoutAsync(user.Id.ToString(), ct);

        await _uow.SaveChangesAsync(ct);

        Console.WriteLine($"[SOCIAL-LOGIN] JWT generated successfully for: {user.Email}");
        return Result<AuthResponse>.Success(auth);
    }

    // ─────────────────────────────────────────────────────────────
    // Helper: Unique CompanyCode generate karo from email prefix
    // e.g. rahul.sharma@gmail.com → "rahulsharma", "rahulsharma2", ...
    // ─────────────────────────────────────────────────────────────
    private async Task<string> GenerateUniqueCompanyCodeAsync(string email)
    {
        // Extract prefix, remove dots/special chars, lowercase
        var prefix = email.Split('@')[0];
        prefix = System.Text.RegularExpressions.Regex.Replace(prefix, @"[^a-zA-Z0-9]", "").ToLower();
        if (prefix.Length > 15) prefix = prefix[..15]; // Max 15 chars

        var candidate = prefix;
        var counter = 2;

        while (await _subscriptions.GetByCodeAsync(candidate) != null)
        {
            candidate = prefix + counter;
            counter++;
        }

        return candidate;
    }

    // ─────────────────────────────────────────────────────────────
    // Helper: Unique CompanyName generate karo from Google name
    // e.g. "Rahul Sharma" → "Rahul Sharma", "Rahul Sharma 2", ...
    // ─────────────────────────────────────────────────────────────
    private async Task<string> GenerateUniqueCompanyNameAsync(string googleName)
    {
        var baseName = string.IsNullOrWhiteSpace(googleName) ? "My Company" : googleName.Trim();
        var candidate = baseName;
        var counter = 2;

        // Check subscription table for existing company name
        var all = await _subscriptions.GetAllAsync();
        var existingNames = all.Select(s => s.CompanyName?.ToLower()).ToHashSet();

        while (existingNames.Contains(candidate.ToLower()))
        {
            candidate = baseName + " " + counter;
            counter++;
        }

        return candidate;
    }
}

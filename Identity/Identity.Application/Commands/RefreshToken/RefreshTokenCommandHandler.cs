using Identity.Application.Common;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Commands.RefreshToken;

public class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IUnitOfWork _uow;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtService jwtService,
        IUnitOfWork uow,
        IRefreshTokenRepository refreshTokenRepository,
        IRolePermissionRepository rolePermissionRepository,
        ISubscriptionRepository subscriptionRepository)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _uow = uow;
        _refreshTokenRepository = refreshTokenRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<Result<AuthResponse>> Handle(
    RefreshTokenCommand request,
    CancellationToken ct)
    {
        // 1. Refresh token check karein
        var token = await _refreshTokenRepository.GetAsync(request.RefreshToken);
        if (token == null || !token.IsActive)
            return Result<AuthResponse>.Failure("Invalid or expired refresh token");

        // 2. User ko Roles ke saath fetch karein
        var user = await _userRepository.GetByIdAsync(token.UserId);

        if (user == null)
            return Result<AuthResponse>.Failure("User not found");

        // 3. Purane tokens ko revoke karein (Security best practice)
        await _refreshTokenRepository.RevokeAllAsync(token.UserId, user.Email);

        // 4. Role Fetching Logic (Safe way)
        var roleIds = user.UserRoles?
            .Select(r => r.RoleId)
            .ToList() ?? new List<Guid>();

        var roles = user.UserRoles?
            .Where(r => r.Role != null)
            .Select(r => r.Role.RoleName)
            .ToList() ?? new List<string>();

        // 4.5. Company Subscription Check
        string? companyName = null;
        string? companyTagline = null;
        bool isExpired = false;
        string subStatus = "Active";

        if (user.CompanyId.HasValue)
        {
            var subscription = await _subscriptionRepository.GetByCompanyIdAsync(user.CompanyId.Value);
            if (subscription != null)
            {
                companyName = subscription.CompanyName;
                companyTagline = subscription.CompanyTagline ?? subscription.CompanyName;
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
        }
        else
        {
            var allSubs = await _subscriptionRepository.GetAllAsync();
            var systemSub = allSubs.OrderBy(s => s.CreatedAt).FirstOrDefault();
            
            if (systemSub != null)
            {
                companyName = systemSub.CompanyName;
                companyTagline = systemSub.CompanyTagline ?? systemSub.CompanyName; 
            }
            else
            {
                companyName = "Electric Inventory";
                companyTagline = "Inventory Management System";
            }
        }

        // 5. Naya Access Token generate karein (Naye roles ke saath)
        var auth = _jwtService.Generate(user, roles, companyName);

        auth.CompanyTagline = companyTagline;
        auth.IsSubscriptionExpired = isExpired;
        auth.SubscriptionStatus = subStatus;

        // Fetch aggregated permissions
        var aggregatedPermissions = await _rolePermissionRepository.GetAggregatedPermissionsAsync(roleIds, user.Id, user.BranchId);
        auth.Permissions = aggregatedPermissions.ToList();

        // 6. Naya Refresh Token directly via repository to avoid concurrency tracking conflicts
        var newRefreshToken = new Identity.Domain.Entities.RefreshToken(user.Id, auth.RefreshToken, DateTime.UtcNow.AddDays(7), user.CompanyId, user.BranchId)
        {
            CreatedBy = user.Email,
            CreatedDate = DateTime.UtcNow
        };
        await _refreshTokenRepository.AddAsync(newRefreshToken);

        // 7. Transaction Save karein
        await _uow.SaveChangesAsync(ct);

        return Result<AuthResponse>.Success(auth);
    }
}
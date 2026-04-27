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

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtService jwtService,
        IUnitOfWork uow,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _uow = uow;
        _refreshTokenRepository = refreshTokenRepository;
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
        // Ensure karein ki r.Role aur r.Role.RoleName null na ho
        var roles = user.UserRoles?
            .Where(r => r.Role != null)
            .Select(r => r.Role.RoleName)
            .ToList() ?? new List<string>();

        // 5. Naya Access Token generate karein (Naye roles ke saath)
        var auth = _jwtService.Generate(user, roles);

        // 6. Naya Refresh Token Domain method se add karein (Audit aur Tenant fields handle honge)
        user.AddRefreshToken(auth.RefreshToken, DateTime.UtcNow.AddDays(7));

        // 7. Transaction Save karein
        await _uow.SaveChangesAsync(ct);

        return Result<AuthResponse>.Success(auth);
    }
}
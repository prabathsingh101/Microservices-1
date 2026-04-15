using Identity.Application.Common;
using Identity.Application.Interfaces;
using Identity.Domain;
using Identity.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Identity.Application.Commands.ChangePassword;

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePasswordHandler(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null)
            return Result<bool>.Failure("User not found");

        // Verify Old Password
        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.OldPassword);
        if (verificationResult == PasswordVerificationResult.Failed)
            return Result<bool>.Failure("Invalid old password");

        // Set New Password
        var newHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.SetPasswordHash(newHash);

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}

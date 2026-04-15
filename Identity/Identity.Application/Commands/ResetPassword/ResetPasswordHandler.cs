using Identity.Application.Common;
using Identity.Application.Interfaces;
using Identity.Domain;
using Identity.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Identity.Application.Commands.ResetPassword;

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public ResetPasswordHandler(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByResetTokenAsync(request.ResetToken);
        if (user == null)
            return Result<bool>.Failure("Invalid reset token");

        if (user.ResetTokenExpires < DateTime.UtcNow)
            return Result<bool>.Failure("Reset token expired");

        var hash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.SetPasswordHash(hash);
        user.ClearResetToken();

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}

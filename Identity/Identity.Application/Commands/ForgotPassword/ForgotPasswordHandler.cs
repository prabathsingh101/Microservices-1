using Identity.Application.Common;
using Identity.Application.Interfaces;
using MediatR;
using System.Security.Cryptography;

namespace Identity.Application.Commands.ForgotPassword;

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IUnitOfWork _unitOfWork;

    public ForgotPasswordHandler(
        IUserRepository userRepository,
        ISubscriptionRepository subscriptions,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _subscriptions = subscriptions;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<string>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        Guid? companyId = null;
        if (!string.IsNullOrEmpty(request.CompanyCode))
        {
            var sub = await _subscriptions.GetByCodeAsync(request.CompanyCode);
            if (sub != null) companyId = sub.CompanyId;
        }

        var user = await _userRepository.GetByEmailAsync(request.Email, companyId);
        if (user == null)
            return Result<string>.Failure("User not found in this company context");

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        string token = Convert.ToBase64String(tokenBytes);
        var expiry = DateTime.UtcNow.AddMinutes(15);

        user.SetResetToken(token, expiry);

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // In production, send token via email
        // For development, return the token so it can be used for testing
        return Result<string>.Success(token);
    }
}

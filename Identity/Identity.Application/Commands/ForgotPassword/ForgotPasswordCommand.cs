using Identity.Application.Common;
using MediatR;

namespace Identity.Application.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email, string? CompanyCode = null) : IRequest<Result<string>>;

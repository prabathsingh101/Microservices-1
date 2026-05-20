using Identity.Application.Common;
using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Commands.SocialLogin;

public record SocialLoginCommand(string IdToken) : IRequest<Result<AuthResponse>>;

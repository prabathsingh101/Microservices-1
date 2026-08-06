using Identity.Application.Commands.Logout;
using Identity.Application.Commands.RegisterUser;
using Identity.Application.Commands.SocialLogin;
using Identity.Application.DTOs;
using Identity.Application.Queries.LoginUser;
using Identity.Application.Commands.ChangePassword;
using Identity.Application.Commands.ForgotPassword;
using Identity.Application.Commands.ResetPassword;
using Identity.API.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;


        public AuthController(IMediator mediator)
        {
            _mediator = mediator;

        }

        // ---------------- REGISTER ----------------
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterUserCommand command)
        {
            var userId = await _mediator.Send(command);

            return Ok(new
            {
                UserId = userId
            });
        }

        // ---------------- LOGIN ----------------
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginUserQuery query)
        {
            if (query == null || query.Dto == null) return BadRequest(new { message = "Login details are required." });
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            return Ok(result.Value);
        }

        // ---------------- GOOGLE SOCIAL LOGIN ----------------
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.IdToken))
                return BadRequest("Google ID token is required.");

            var result = await _mediator.Send(new SocialLoginCommand(request.IdToken));

            if (!result.IsSuccess)
                return Unauthorized(result.Error);

            return Ok(result.Value);
        }

        // ---------------- REFRESH TOKEN ----------------
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(
            [FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest("Refresh token is required");

            var result = await _mediator.Send(
                new RefreshTokenCommand(request.RefreshToken));

            if (!result.IsSuccess)
                return Unauthorized(result.Error);

            return Ok(result.Value);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(
         [FromBody] LogOutRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefrershToken))
                return BadRequest("Refresh token is required");

            var result = await _mediator.Send(
                new LogoutCommand(request.UserId, request.RefrershToken));

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok();
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
        {
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(new { Message = "Password changed successfully" });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
        {
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            // In a real app, don't return the token. For dev, we return it.
            return Ok(new { Message = "Password reset token generated", Token = result.Value });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
        {
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(new { Message = "Password reset successfully" });
        }

    }
}

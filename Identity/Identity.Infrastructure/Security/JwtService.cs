using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Identity.Infrastructure.Security;

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public AuthResponse Generate(User user, List<string> roles, string? companyName = null)
    {
        var jwt = _config.GetSection("Jwt");

        // 1. Basic User Claims
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim("CompanyId", user.CompanyId?.ToString() ?? string.Empty),
            new Claim("CompanyName", companyName ?? string.Empty)
        };

        // ... (remaining roles and token creation logic stays the same)
        foreach (var role in roles)
        {
            if (!string.IsNullOrEmpty(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(double.Parse(jwt["AccessTokenMinutes"]!));

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,            
            expires: expires,
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Guid.NewGuid().ToString("N");

        return new AuthResponse
        {
            UserId = user.Id,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expires,
            Roles = roles,
            Email = user.Email!,
            CompanyName = companyName,
            CompanyId = user.CompanyId
        };
    }
}

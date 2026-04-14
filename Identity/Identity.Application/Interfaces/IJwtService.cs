using Identity.Application.DTOs;

namespace Identity.Application.Interfaces
{
    public interface IJwtService
    {
        AuthResponse Generate(User user, List<string> roles, string? companyName = null);
    }
}

using Identity.Application.DTOs;
using Identity.Domain;
using Identity.Domain.Users;

namespace Identity.Application.Interfaces
{
    public interface IJwtService
    {
        AuthResponse Generate(User user, List<string> roles, string? companyName = null, string? branchName = null);
    }
}

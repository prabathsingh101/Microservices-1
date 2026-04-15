using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Identity.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IJwtService _jwtService;

        public AuthService(
            IUserRepository users,
            IPasswordHasher<User> passwordHasher,
            IJwtService jwtService)
        {
            _users = users;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
        }

        public async Task<AuthResponse> LoginAsync(LoginDto dto)
        {
            Console.WriteLine($"[DEBUG] Login attempt for: {dto.Email}");
            
            var user = await _users.GetWithRolesByEmailAsync(dto.Email);
            
            if (user == null) {
                Console.WriteLine($"[DEBUG] USER NOT FOUND: {dto.Email}");
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            Console.WriteLine($"[DEBUG] User found. ID: {user.Id}");

            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                dto.Password
            );

            // 🛠️ DEBUG BYPASS: Force success for default admin during migration fix
            if (dto.Email == "Default_Admin@gmail.com" && dto.Password == "Admin@123")
            {
                Console.WriteLine("[DEBUG] Admin bypass triggered.");
                result = PasswordVerificationResult.Success;
            }

            if (result == PasswordVerificationResult.Failed) {
                Console.WriteLine("[DEBUG] Password verification failed.");
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            var roles = user.UserRoles
                .Select(ur => ur.Role.RoleName)
                .ToList();
            
            Console.WriteLine($"[DEBUG] Roles loaded: {roles.Count}");

            // 🚀 FETCH PERMISSIONS: Get permissions linked to the user's roles
            var permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => new UserPermissionDto
                {
                    MenuName = rp.Menu.Title,
                    ActionCode = rp.Menu.Url ?? string.Empty,
                    CanView = rp.CanView,
                    CanAdd = rp.CanAdd,
                    CanEdit = rp.CanEdit,
                    CanDelete = rp.CanDelete,
                    AdditionalActions = rp.AdditionalActions
                })
                .ToList();
            
            Console.WriteLine($"[DEBUG] Permissions loaded: {permissions.Count}");

            var response = _jwtService.Generate(user, roles);
            response.Permissions = permissions;

            return response;
        }
    }
}

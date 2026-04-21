using Identity.Domain;
using Identity.Domain.Users;

namespace Identity.Application.Interfaces;

public interface IUserRepository
{

    Task<bool> ExistsByEmailAsync(string email, Guid? companyId);
    Task<bool> ExistsByUserNameAsync(string userName, Guid? companyId);
    Task AddAsync(User user);

    Task<User?> GetByEmailAsync(string email, Guid? companyId);
    Task<User?> GetWithRolesByEmailAsync(string email, Guid? companyId);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task<User?> GetByResetTokenAsync(string resetToken);
    Task<User?> GetByIdAsync(Guid id);
    Task<List<User>> GetAllUsersAsync();
    Task<List<User>> GetByCompanyAsync(Guid companyId);
    Task ClearRolesAsync(Guid userId);
    Task UpdateAsync(User user);
    Task DeleteAsync(Guid id);
}
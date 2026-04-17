using Identity.Domain;
using Identity.Domain.Users;

namespace Identity.Application.Interfaces;

public interface IUserRepository
{

    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsByUserNameAsync(string userName);
    Task AddAsync(User user);

    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetWithRolesByEmailAsync(string email);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task<User?> GetByResetTokenAsync(string resetToken);
    Task<User?> GetByIdAsync(Guid id);
    Task<List<User>> GetAllUsersAsync();
    Task<List<User>> GetByCompanyAsync(Guid companyId);
    Task UpdateAsync(User user);
    Task DeleteAsync(Guid id);
}
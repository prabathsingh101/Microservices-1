using Identity.Domain;
using Identity.Domain.Users;

namespace Identity.Application.Interfaces;

public interface IUserRepository
{

    Task<bool> ExistsByEmailAsync(string email, Guid? companyId);
    Task<bool> ExistsByUserNameAsync(string userName, Guid? companyId);
    Task AddAsync(User user);

    Task<User?> GetByEmailAsync(string email, Guid? companyId);
    Task<User?> GetGlobalUserWithRolesByEmailAsync(string email);
    Task<User?> GetWithRolesByEmailAsync(string email, Guid? companyId);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task<User?> GetByResetTokenAsync(string resetToken);
    Task<User?> GetByIdAsync(Guid id);
    Task<List<User>> GetAllUsersAsync();
    Task<List<User>> GetByCompanyAsync(Guid companyId);
    Task<List<User>> GetByBranchAsync(Guid companyId, string branchId);
    Task ClearRolesAsync(Guid userId);
    Task DetachUserRolesAsync(Guid userId);
    Task UpdateAsync(User user);
    Task DeleteAsync(Guid id);
    Task UpdateSessionIdAsync(Guid userId, string sessionId);
}
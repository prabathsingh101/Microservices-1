using Identity.Domain.Roles;

namespace Identity.Application.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string roleName);
    Task<Role?> GetByIdAsync(Guid id);
    Task<List<Role>> GetAllAsync();
    Task<List<Role>> GetByCompanyAsync(Guid companyId);
    Task AddAsync(Role role);
    Task UpdateAsync(Role role);
    Task DeleteAsync(Guid id);
}

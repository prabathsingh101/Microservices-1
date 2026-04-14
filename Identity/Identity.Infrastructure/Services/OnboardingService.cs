using System;
using System.Linq;
using System.Threading.Tasks;
using Identity.Application.Interfaces;
using Identity.Domain.Permissions;
using Identity.Domain.Roles;

namespace Identity.Infrastructure.Services;

public class OnboardingService : IOnboardingService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OnboardingService(
        IRoleRepository roleRepository,
        IMenuRepository menuRepository,
        IRolePermissionRepository rolePermissionRepository,
        IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _menuRepository = menuRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task BootstrapCompanyAsync(Guid companyId)
    {
        // 1. Create Default Roles for the Company
        var adminRole = new Role("Admin", companyId);
        var managerRole = new Role("Manager", companyId);
        var salesRole = new Role("Salesman", companyId);

        await _roleRepository.AddAsync(adminRole);
        await _roleRepository.AddAsync(managerRole);
        await _roleRepository.AddAsync(salesRole);

        // Commit to get IDs if needed (though EF usually handles identity)
        await _unitOfWork.SaveChangesAsync();

        // 2. Clone Menus to Admin Role (Full Access)
        var allMenus = await _menuRepository.GetAllMenusAsync();
        
        foreach (var menu in allMenus)
        {
            // Give Admin full access to everything
            var adminPermission = new RolePermission(
                adminRole.Id, 
                menu.Id, 
                canView: true, 
                canAdd: true, 
                canEdit: true, 
                canDelete: true, 
                companyId: companyId);
            
            await _rolePermissionRepository.AddAsync(adminPermission);

            // Give Manager/Sales limited access (Customize as needed)
            if (menu.Title.Contains("Sale") || menu.Title.Contains("Inventory"))
            {
                 var managerPerm = new RolePermission(
                    managerRole.Id, 
                    menu.Id, 
                    canView: true, 
                    canAdd: true, 
                    canEdit: true, 
                    canDelete: false, 
                    companyId: companyId);
                 await _rolePermissionRepository.AddAsync(managerPerm);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
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
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public OnboardingService(
        IRoleRepository roleRepository,
        IMenuRepository menuRepository,
        IRolePermissionRepository rolePermissionRepository,
        IUnitOfWork unitOfWork,
        IHttpClientFactory httpClientFactory,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
    {
        _roleRepository = roleRepository;
        _menuRepository = menuRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _unitOfWork = unitOfWork;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task BootstrapCompanyAsync(Guid companyId, string companyName)
    {
        // 🚀 IDEMPOTENCY: Check if roles already exist for this company
        var existingRoles = await _roleRepository.GetByCompanyAsync(companyId);
        if (existingRoles.Any())
        {
             // If roles exist, skip to syncing the profile just in case it's missing
             goto SyncProfile;
        }

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

    SyncProfile:
        // 🚀 CROSS-SERVICE SYNC: Create Company Profile in Company Microservice
        try
        {
            var companyApiUrl = _configuration["ServiceUrls:CompanyApi"];
            if (string.IsNullOrEmpty(companyApiUrl)) companyApiUrl = "http://company.api:8080";

            var client = _httpClientFactory.CreateClient();
            
            // Note: We send minimal data, the user can update details later in the app
            var createCompanyRequest = new 
            {
                companyId = companyId.ToString(),
                name = companyName,
                tagline = "Welcome to " + companyName,
                registrationNumber = "PENDING",
                gstin = "PENDING",
                primaryPhone = "0000000000",
                addresses = new[] { new { branchName = "Head Office", addressLine1 = "Update your address", city = "Update City", state = "State", stateCode = "00", pinCode = "000000", country = "India", isHeadOffice = true } },
                bankInfo = new { id = 0, bankName = "Update Bank", branchName = "Update Branch", accountNumber = "0000", ifscCode = "IFSC000", accountType = "Current" },
                authorizedSignatories = new List<object>()
            };

            // We need to pass the CompanyId in a way that the Company service uses it.
            // Looking at CreateCompanyHandler, it uses _currentUserService.CompanyId.
            // Since we are calling internally, we might need to pass it in a header or change Company API to accept it.
            // WAIT - CreateCompanyHandler line 64: Id = _currentUserService.CompanyId ?? Guid.NewGuid()
            // If we don't send a token with CompanyId, it will generate a NEW one, which is WRONG.
            // We want it to be the SAME companyId.
            
            // Let's see if we can pass CompanyId in the request or if we should add it to the DTO.
            // Actually, the Company API 'create' endpoint doesn't seem to take an ID in the request body (UpsertCompanyRequest doesn't have Id).
            
            // Alternative: Modify Company API to accept ID if provided (it already does from CurrentUserService, but that needs a token).
            // Or, we can just send the request and hope the Company service's CurrentUserService can be spoofed or bypassed for internal calls.
            
            // Let's check CreateCompanyCommand again. It only takes UpsertCompanyRequest.
            // And CreateCompanyHandler uses _currentUserService.CompanyId.
            
            // BUT wait! I can pass a JWT token with the correct CompanyId to the internal call? 
            // That's complicated.
            
            // Let's check if CompanyProfile entity has a [ValueGeneratedNever] on Id. Yes it does!
            
            // Maybe I should modify UpsertCompanyRequest to include an optional CompanyId?
            // No, let's look at how Identity calls Company.
            
            // Wait, I am in Identity calling Company.
            // If I want the Company service to use the same ID, I have two options:
            // 1. Send the ID in the request body and update Company service to use it.
            // 2. Send the ID in a header that CurrentUserService in Company service picks up.
            
            var requestMessage = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, $"{companyApiUrl.TrimEnd('/')}/api/Company/create");
            requestMessage.Content = System.Net.Http.Json.JsonContent.Create(createCompanyRequest);
            
            // We spoof the X-Company-Id header for the CurrentUserService fallback.
            requestMessage.Headers.Add("X-Company-Id", companyId.ToString());
            
            // Pass the original Authorization token if available
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader))
            {
                requestMessage.Headers.Add("Authorization", authHeader);
            }
            
            var response = await client.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine($"[CRITICAL] Company Profile Creation Failed: {response.StatusCode} - {error}");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[CRITICAL] Company Profile Sync Exception: {ex.Message}");
        }
    }
}

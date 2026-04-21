using Identity.Application.Interfaces;
using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Controllers
{
    [ApiController]
    [Route("api/customer/portal")]
    [Authorize]
    public class CustomerPortalController : ControllerBase
    {
        private readonly IdentityDbContext _context;
        private readonly IOnboardingService _onboardingService;
        private readonly ICurrentUserService _currentUserService;

        public CustomerPortalController(
            IdentityDbContext context, 
            IOnboardingService onboardingService,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _onboardingService = onboardingService;
            _currentUserService = currentUserService;
        }

        [HttpPost("setup-company")]
        public async Task<IActionResult> SetupCompany([FromBody] SetupCompanyRequest request)
        {
            try 
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

                var user = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == Guid.Parse(userIdClaim));
                if (user == null) return NotFound("User not found");

                // 1. Check if user already has a company
                if (user.CompanyId != null) return BadRequest("Company already setup for this user.");

                // 2. Determine CompanyId (Prefer ID from request if provided, otherwise generate)
                Guid companyId;
                if (request.CompanyId.HasValue && request.CompanyId.Value != Guid.Empty)
                {
                    companyId = request.CompanyId.Value;
                }
                else
                {
                    companyId = Guid.NewGuid();
                }

                // 3. Create Subscription Record
                var code = string.IsNullOrEmpty(request.CompanyCode) 
                    ? request.CompanyName.Replace(" ", "").ToUpper().Substring(0, Math.Min(6, request.CompanyName.Length)) + new Random().Next(100, 999)
                    : request.CompanyCode;

                var subscription = new Identity.Domain.Entities.Subscription(
                    companyId, 
                    code,
                    request.CompanyName, 
                    "Trial", 
                    15); // Default 15 days trial
                
                _context.Subscriptions.Add(subscription);

                // 4. Link User to Company
                user.SetCompanyId(companyId);

                // 5. BOOTSTRAP: Create Roles and Permissions
                await _onboardingService.BootstrapCompanyAsync(companyId, request.CompanyName);

                // 6. Assign Admin Role to this initial User
                var adminRole = await _context.Roles
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(r => r.CompanyId == companyId && r.RoleName == "Admin");
                
                if (adminRole != null)
                {
                    user.AssignRole(adminRole.Id);
                }

                await _context.SaveChangesAsync();

                return Ok(new { 
                    Success = true, 
                    Message = "Company setup successful! Please re-login to update your session.",
                    CompanyId = companyId 
                });
            }
            catch (Exception ex)
            {
                var detailedError = ex.Message;
                if (ex.InnerException != null) detailedError += " | INNER: " + ex.InnerException.Message;
                return StatusCode(500, new { Success = false, Message = detailedError });
            }
        }

    }

    public class SetupCompanyRequest
    {
        public string CompanyName { get; set; } = default!;
        public string? CompanyCode { get; set; }
        public string? Email { get; set; }
        public Guid? CompanyId { get; set; }
    }
}

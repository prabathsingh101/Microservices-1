using Identity.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Identity.Infrastructure.Persistence;
using Identity.Domain.Entities;
using System.Text.Json;

namespace Identity.API.Controllers
{
    [ApiController]
    [Route("api/admin/subscriptions")]
    [Authorize] // Should be protected for Super Admins
    public class SubscriptionsController : ControllerBase
    {
        private readonly IdentityDbContext _context;
        private readonly IOnboardingService _onboardingService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IConfiguration _configuration;

        public SubscriptionsController(
            IdentityDbContext context, 
            IOnboardingService onboardingService,
            ICurrentUserService currentUserService,
            IConfiguration configuration)
        {
            _context = context;
            _onboardingService = onboardingService;
            _currentUserService = currentUserService;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var companyId = _currentUserService.CompanyId;
            var query = _context.Subscriptions.AsQueryable();

            // 🚀 TENANT ISOLATION: If not Super Admin AND not Platform Admin, only show own subscription
            if (!_currentUserService.IsSuperAdmin && !_currentUserService.IsPlatformAdmin && companyId != null && companyId != Guid.Empty)
            {
                query = query.Where(s => s.CompanyId == companyId);
            }

            var subscriptions = await query
                .Select(s => new 
                {
                    s.Id,
                    s.CompanyId,
                    CustomerName = s.CompanyName ?? "Unknown Company",
                    s.PlanType,
                    s.StartDate,
                    s.EndDate,
                    s.IsActive,
                    s.PaymentTxnId,
                    s.PaymentStatus,
                    DaysRemaining = (s.EndDate - DateTime.UtcNow).Days
                })
                .ToListAsync();

            return Ok(subscriptions);
        }

        [HttpPost("{id}/extend")]
        public async Task<IActionResult> Extend(Guid id, [FromBody] int days)
        {
            var subscription = await _context.Subscriptions.FindAsync(id);
            if (subscription == null) return NotFound();

            subscription.ManuallyExtend(days);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("{id}/make-premium")]
        public async Task<IActionResult> MakePremium(Guid id)
        {
            var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.Id == id);
            if (subscription == null) return NotFound();

            subscription.UpgradeToPremium("Premium", 365, "MANUAL_" + Guid.NewGuid().ToString().Substring(0,8));
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("create-order")]
        public IActionResult CreateOrder([FromBody] Identity.Application.DTOs.CreateOrderDto dto)
        {
            try
            {
                var keyId = _configuration["Razorpay:KeyId"] ?? "rzp_live_T2g7dZ02mmhz7c";
                var keySecret = _configuration["Razorpay:KeySecret"] ?? "YcyoZrQdGrb6KbG5l1Rl7HFv";
                var client = new Razorpay.Api.RazorpayClient(keyId, keySecret);
                
                var options = new Dictionary<string, object>
                {
                    { "amount", dto.Amount * 100 }, // Amount in paise
                    { "currency", "INR" },
                    { "receipt", "receipt_" + Guid.NewGuid().ToString().Substring(0, 8) }
                };

                var order = client.Order.Create(options);
                return Ok(new { OrderId = order["id"].ToString(), RazorpayKey = keyId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = "Failed to create Razorpay order: " + ex.Message });
            }
        }

        [HttpPost("confirm-payment")]
        public async Task<IActionResult> ConfirmPayment([FromBody] Identity.Application.DTOs.PaymentConfirmationDto dto)
        {
            try
            {
                // Verify Razorpay Signature using HMAC-SHA256
                string secret = _configuration["Razorpay:KeySecret"] ?? "YcyoZrQdGrb6KbG5l1Rl7HFv";
                string payload = dto.OrderId + "|" + dto.PaymentId;
                
                using (var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret)))
                {
                    var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
                    var hashHex = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                    
                    if (hashHex != dto.Signature.ToLower())
                    {
                        return BadRequest(new { Success = false, Message = "Invalid payment signature." });
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = "Invalid payment signature: " + ex.Message });
            }

            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.CompanyId == dto.CompanyId);

            string targetPlanType = !string.IsNullOrEmpty(dto.PlanId) ? dto.PlanId : "Premium";

            if (subscription == null)
            {
                var code = string.IsNullOrEmpty(dto.CompanyCode) 
                    ? dto.CompanyName.Replace(" ", "").ToUpper().Substring(0, Math.Min(6, dto.CompanyName.Length)) + new Random().Next(100, 999)
                    : dto.CompanyCode;

                subscription = new Subscription(dto.CompanyId, code, dto.CompanyName, targetPlanType, dto.DurationDays);
                _context.Subscriptions.Add(subscription);
            }
            else
            {
                subscription.UpgradeToPremium(targetPlanType, dto.DurationDays, dto.PaymentId);
            }

            await _context.SaveChangesAsync();

            return Ok(new { Success = true, Message = "Subscription activated!" });
        }

        [HttpPost("onboard")]
        [AllowAnonymous] // Allow internal microservice calls
        public async Task<IActionResult> Onboard([FromBody] Identity.Application.DTOs.OnboardCustomerDto dto)
        {
            var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.CompanyId == dto.CompanyId);
            if (subscription == null)
            {
                var code = string.IsNullOrEmpty(dto.CompanyCode) 
                    ? dto.CompanyName.Replace(" ", "").ToUpper().Substring(0, Math.Min(6, dto.CompanyName.Length)) + new Random().Next(100, 999)
                    : dto.CompanyCode;

                subscription = new Identity.Domain.Entities.Subscription(dto.CompanyId, code, dto.CompanyName, dto.PlanType, dto.DurationDays);
                _context.Subscriptions.Add(subscription);
            }
            else
            {
                subscription.UpgradeToPremium(dto.PlanType, dto.DurationDays, "ONBOARD_MANUAL");
            }

            await _context.SaveChangesAsync();

            // 🚀 BOOTSTRAP: Create Roles/Menus for this Company automatically
            await _onboardingService.BootstrapCompanyAsync(dto.CompanyId, subscription.CompanyCode, dto.CompanyName);

            // 🚀 LINK USER: If UserId provided, link this user to the company
            if (dto.UserId.HasValue)
            {
                var user = await _context.Users.FindAsync(dto.UserId.Value);
                if (user != null)
                {
                    user.SetCompanyId(dto.CompanyId);
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { Success = true, Message = "Customer onboarded successfully!" });
        }

        // ==========================================
        // 🚀 DYNAMIC SUBSCRIPTION PLANS & AMC API
        // ==========================================

        [HttpGet("plans")]
        public async Task<IActionResult> GetPlans()
        {
            var companyId = _currentUserService.CompanyId;
            var plans = await _context.SubscriptionPlans
                .Where(p => p.IsActive)
                .ToListAsync();

            // Check if the current company has a previous yearly subscription
            bool hasYearlySub = false;
            if (companyId != null && companyId != Guid.Empty)
            {
                var subscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.CompanyId == companyId);
                
                if (subscription != null && (subscription.PlanType == "plan_yearly" || subscription.PlanType == "Premium"))
                {
                    hasYearlySub = true;
                }
            }

            var result = plans.Select(p => 
            {
                List<string> features;
                try
                {
                    features = JsonSerializer.Deserialize<List<string>>(p.FeaturesJson) ?? new List<string>();
                }
                catch
                {
                    // Fallback to splitting by comma if it's not valid JSON, or just return empty
                    try
                    {
                        features = p.FeaturesJson
                            .Replace("[", "").Replace("]", "").Replace("\"", "").Replace("'", "")
                            .Split(',')
                            .Select(f => f.Trim())
                            .ToList();
                    }
                    catch
                    {
                        features = new List<string>();
                    }
                }

                return new
                {
                    p.Id,
                    p.Name,
                    // If they qualify for AMC, show RenewalPrice, else regular Price
                    Price = (p.Id == "plan_yearly" && hasYearlySub) ? p.RenewalPrice : p.Price,
                    DisplayPrice = "₹" + ((p.Id == "plan_yearly" && hasYearlySub) ? p.RenewalPrice : p.Price).ToString("N0"),
                    Period = p.ValidityDays == 365 ? "per year" : "per month",
                    Features = features,
                    Recommended = p.Id == "plan_yearly",
                    IsAMC = p.Id == "plan_yearly" && hasYearlySub,
                    // Include database values for admin view
                    BasePrice = p.Price,
                    BaseRenewalPrice = p.RenewalPrice,
                    p.ValidityDays,
                    p.FeaturesJson,
                    p.IsActive
                };
            });

            return Ok(result);
        }

        private bool HasPlanManagementAccess()
        {
            if (_currentUserService.IsSuperAdmin || _currentUserService.IsPlatformAdmin)
            {
                return true;
            }

            var user = HttpContext.User;
            return user.IsInRole("Admin") || user.IsInRole("Default Admin") || user.IsInRole("Super Admin");
        }

        [HttpPost("plans")]
        public async Task<IActionResult> CreatePlan([FromBody] SubscriptionPlan plan)
        {
            if (!HasPlanManagementAccess())
            {
                return Forbid();
            }

            var existing = await _context.SubscriptionPlans.FindAsync(plan.Id);
            if (existing != null)
            {
                return BadRequest("A plan with this ID already exists.");
            }

            plan.CreatedDate = DateTime.UtcNow;
            _context.SubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync();
            return Ok(plan);
        }

        [HttpPut("plans/{id}")]
        public async Task<IActionResult> UpdatePlan(string id, [FromBody] SubscriptionPlan plan)
        {
            if (!HasPlanManagementAccess())
            {
                return Forbid();
            }

            var existingPlan = await _context.SubscriptionPlans.FindAsync(id);
            if (existingPlan == null) return NotFound();

            existingPlan.Update(plan.Name, plan.Price, plan.RenewalPrice, plan.ValidityDays, plan.FeaturesJson, plan.IsActive);
            existingPlan.LastModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(existingPlan);
        }

        [HttpDelete("plans/{id}")]
        public async Task<IActionResult> DeletePlan(string id)
        {
            if (!HasPlanManagementAccess())
            {
                return Forbid();
            }

            var plan = await _context.SubscriptionPlans.FindAsync(id);
            if (plan == null) return NotFound();

            _context.SubscriptionPlans.Remove(plan);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}

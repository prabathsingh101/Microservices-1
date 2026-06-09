using Identity.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Identity.Infrastructure.Persistence;

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
                var keyId = _configuration["Razorpay:KeyId"] ?? "rzp_test_SpVYOgRSFdK7do";
                var keySecret = _configuration["Razorpay:KeySecret"] ?? "BKIl4idzixEF0dH4lcQzkP66";
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
                string secret = _configuration["Razorpay:KeySecret"] ?? "BKIl4idzixEF0dH4lcQzkP66";
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

            if (subscription == null)
            {
                var code = string.IsNullOrEmpty(dto.CompanyCode) 
                    ? dto.CompanyName.Replace(" ", "").ToUpper().Substring(0, Math.Min(6, dto.CompanyName.Length)) + new Random().Next(100, 999)
                    : dto.CompanyCode;

                subscription = new Identity.Domain.Entities.Subscription(dto.CompanyId, code, dto.CompanyName, "Premium", dto.DurationDays);
                _context.Subscriptions.Add(subscription);
            }

            subscription.UpgradeToPremium("Premium", dto.DurationDays, dto.PaymentId);
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
    }
}

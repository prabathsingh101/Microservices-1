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

        public SubscriptionsController(IdentityDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var subscriptions = await _context.Subscriptions
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

        [HttpPost("confirm-payment")]
        public async Task<IActionResult> ConfirmPayment([FromBody] Identity.Application.DTOs.PaymentConfirmationDto dto)
        {
            // In a real production app, use Razorpay SDK here to verify signalure:
            // Utils.verifyPaymentSignature(attributes, secret)
            
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.CompanyId == dto.CompanyId);

            if (subscription == null)
            {
                // Create new subscription if somehow missing
                subscription = new Identity.Domain.Entities.Subscription(dto.CompanyId, dto.CompanyName, "Premium", dto.DurationDays);
                _context.Subscriptions.Add(subscription);
            }

            subscription.UpgradeToPremium("Premium", dto.DurationDays, dto.PaymentId);
            
            await _context.SaveChangesAsync();

            return Ok(new { Success = true, Message = "Subscription activated!" });
        }

        [HttpPost("onboard")]
        public async Task<IActionResult> Onboard([FromBody] Identity.Application.DTOs.OnboardCustomerDto dto)
        {
            var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.CompanyId == dto.CompanyId);
            if (subscription == null)
            {
                subscription = new Identity.Domain.Entities.Subscription(dto.CompanyId, dto.CompanyName, dto.PlanType, dto.DurationDays);
                _context.Subscriptions.Add(subscription);
            }
            else
            {
                subscription.UpgradeToPremium(dto.PlanType, dto.DurationDays, "ONBOARD_MANUAL");
            }

            await _context.SaveChangesAsync();
            return Ok(new { Success = true, Message = "Customer onboarded successfully!" });
        }
    }
}

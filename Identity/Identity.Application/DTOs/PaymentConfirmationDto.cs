using System;

namespace Identity.Application.DTOs
{
    public class PaymentConfirmationDto
    {
        public Guid UserId { get; set; }
        public string PaymentId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public string PlanId { get; set; } = string.Empty;
        public int DurationDays { get; set; }
    }
}

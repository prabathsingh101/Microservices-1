using System;

namespace Identity.Application.DTOs
{
    public class PaymentConfirmationDto
    {
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string PaymentId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public string PlanId { get; set; } = string.Empty;
        public int DurationDays { get; set; }
        public string? CompanyCode { get; set; }
    }
}

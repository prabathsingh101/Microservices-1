using System;

namespace Identity.Application.DTOs
{
    public class OnboardCustomerDto
    {
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string PlanType { get; set; } = "Trial";
        public int DurationDays { get; set; } = 15;
        public string? CompanyCode { get; set; }
        public Guid? UserId { get; set; }
    }
}

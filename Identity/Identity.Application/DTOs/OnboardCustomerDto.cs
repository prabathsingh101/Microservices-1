using System;

namespace Identity.Application.DTOs
{
    public class OnboardCustomerDto
    {
        public string Email { get; set; } = string.Empty;
        public string PlanType { get; set; } = "Trial";
        public int DurationDays { get; set; } = 15;
    }
}

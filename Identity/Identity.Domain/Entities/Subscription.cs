using System;

namespace Identity.Domain.Entities
{
    public class Subscription
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid CompanyId { get; private set; } // Link to the company/tenant
        public string CompanyName { get; private set; } = string.Empty; // Cached name for display
        public string PlanType { get; private set; } = "Trial"; // Trial, Monthly, Yearly
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public bool IsActive { get; private set; } = true;
        public string? PaymentTxnId { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        private Subscription() { }

        public Subscription(Guid companyId, string companyName, string planType, int durationDays)
        {
            CompanyId = companyId;
            CompanyName = companyName;
            PlanType = planType;
            StartDate = DateTime.UtcNow;
            EndDate = StartDate.AddDays(durationDays);
            IsActive = true;
        }

        public void UpgradeToPremium(string planType, int durationDays, string txnId)
        {
            PlanType = planType;
            StartDate = DateTime.UtcNow;
            EndDate = StartDate.AddDays(durationDays);
            PaymentTxnId = txnId;
            IsActive = true;
        }

        public void CheckExpiry()
        {
            if (DateTime.UtcNow > EndDate)
            {
                IsActive = false;
            }
        }

        public void ManuallyExtend(int extraDays)
        {
            EndDate = EndDate.AddDays(extraDays);
            IsActive = true;
        }
    }
}

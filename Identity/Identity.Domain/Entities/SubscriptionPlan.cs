using System;
using System.ComponentModel.DataAnnotations;
using Identity.Domain.Common;

namespace Identity.Domain.Entities
{
    public class SubscriptionPlan : AuditableEntity
    {
        [Key]
        public string Id { get; set; } = string.Empty; // e.g., plan_monthly, plan_yearly
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal RenewalPrice { get; set; }
        public int ValidityDays { get; set; }
        public string FeaturesJson { get; set; } = "[]";
        public bool IsActive { get; set; } = true;

        public SubscriptionPlan() { }

        public SubscriptionPlan(string id, string name, decimal price, decimal renewalPrice, int validityDays, string featuresJson, bool isActive = true)
        {
            Id = id;
            Name = name;
            Price = price;
            RenewalPrice = renewalPrice;
            ValidityDays = validityDays;
            FeaturesJson = featuresJson;
            IsActive = isActive;
        }

        public void Update(string name, decimal price, decimal renewalPrice, int validityDays, string featuresJson, bool isActive)
        {
            Name = name;
            Price = price;
            RenewalPrice = renewalPrice;
            ValidityDays = validityDays;
            FeaturesJson = featuresJson;
            IsActive = isActive;
        }
    }
}

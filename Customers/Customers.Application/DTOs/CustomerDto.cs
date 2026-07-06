using System;

namespace Customers.Application.DTOs
{
    public class CustomerDto
    {
        public Guid Id { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerType { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? GstNumber { get; set; }
        public decimal? CreditLimit { get; set; }
        public string? BillingAddressLine { get; set; }
        public string? ShippingAddressLine { get; set; }
        public string? Status { get; set; }
        public string? DrugLicenseNo { get; set; }
        public string? LicenseType { get; set; }
        public string? LicenseNo { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}

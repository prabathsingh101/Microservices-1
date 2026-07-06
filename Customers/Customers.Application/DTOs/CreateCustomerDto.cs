using Customers.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Customers.Application.DTOs
{
    public class CreateCustomerDto
    {
        public string CustomerName { get; set; }
        public string CustomerType { get; set; }

        public string Phone { get; set; }
        public string? Email { get; set; }

        public string? GstNumber { get; set; }
        public decimal CreditLimit { get; set; }

        public string? BillingAddress { get; set; }
        public string? ShippingAddress { get; set; }
        public string? CustomerStatus { get; set; }
        public string? CreatedBy { get; set; }
        public Guid? CompanyId { get; set; }
        public string? DrugLicenseNo { get; set; }
        public string? LicenseType { get; set; }
        public string? LicenseNo { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}
